# Self-reposting `BeginInvoke` не дожидается окончания shutdown

## Описание проблемы

Для отложенного выполнения операций модификации дерева (см. [`06-runtime-tree-modification-forbidden.md`](06-runtime-tree-modification-forbidden.md)) первая интуитивная реализация — запостить делегат через `MasterSCADAHlp.Instance.ThreadHolder.BeginInvoke()` и, если в момент его исполнения `IProjectHlp.InRuntime` всё ещё `true`, перепланировать тот же делегат через `BeginInvoke` с уменьшением счётчика retries.

**Симптом:** Делегат «выгорает» за микросекунды. Все retries (например, 100) исчерпываются почти мгновенно, тогда как фактический shutdown MasterSCADA занимает секунды (показывается `ShutdownForm`, останавливаются потоки, выполняются cleanup-скрипты). В лог уходит сообщение об исчерпании retries, реальное выполнение плана так и не происходит.

## Причина

Делегаты, запостенные через `BeginInvoke` на STA-thread, исполняются **back-to-back без задержки** между итерациями. Каждый retry мгновенно перепланирует следующий, сообщения копятся в очереди и обрабатываются подряд — без выхода управления к обработчикам других сообщений Windows.

Между тем shutdown-последовательность MasterSCADA (`rrsAfterStopThreads → put_DesignMode → rrsDoneRuntime → RTManager.Done`) развивается во времени и параллельно качает свой собственный message loop через модальный `ShutdownForm`. Простой `BeginInvoke`-loop не уступает квант времени и не даёт шансу `_inRuntime` смениться на `false`.

## Workaround: `System.Windows.Forms.Timer` с интервалом

Использовать `WM_TIMER`-сообщения вместо `WM_PAINT`-подобных через `BeginInvoke`. `WM_TIMER` имеет низкий приоритет в очереди сообщений и обрабатывается только тогда, когда других сообщений нет, что даёт shutdown-последовательности возможность завершиться.

Параметры по умолчанию: `Interval = 200ms`, `MaxRetries = 100` → суммарный таймаут ~20 секунд.

```csharp
private const int MaxDeferredRetries = 100;
private const int DeferredRetryIntervalMs = 200;

private static void PostDeferredExecution(
    IMyService service, MyPlan plan, Logger? logger,
    IProjectHlp project, int retriesRemaining)
{
    var timer = new Timer { Interval = DeferredRetryIntervalMs };
    var retries = retriesRemaining;

    timer.Tick += (_, _) =>
    {
        if (project.InRuntime)
        {
            retries--;
            if (retries <= 0)
            {
                timer.Stop();
                timer.Dispose();
                logger?.Error(
                    "Deferred execution aborted: InRuntime still true after {Max} retries",
                    MaxDeferredRetries);
                logger?.Dispose();
            }
            return;
        }

        timer.Stop();
        timer.Dispose();
        try
        {
            service.Execute(plan);
        }
        finally
        {
            logger?.Dispose();
        }
    };

    timer.Start();
}
```

## Чего нельзя делать

- Использовать `BeginInvoke` для self-reposting retry-loop'а — он не даёт паузы между попытками и игнорирует фактическое время shutdown.
- Делать `Thread.Sleep` внутри делегата на STA-thread — это блокирует обработку сообщений, в том числе и `ShutdownForm`'а, что может привести к deadlock'у.
- Понижать `MaxRetries` без понимания: 20 секунд — это не запас, а наблюдаемая длительность shutdown в реальных проектах.

## Ссылки

- Реализация: `NtoLib/LinkSwitcher/LinkSwitcherFB.cs`, `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Связанные проблемы: [06](06-runtime-tree-modification-forbidden.md), [07](07-fb-instance-replacement.md)
