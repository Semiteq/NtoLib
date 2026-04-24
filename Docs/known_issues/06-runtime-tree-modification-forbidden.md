# Запрет на модификацию дерева проекта во время Runtime

## Описание проблемы

MasterSCADA запрещает любые операции, изменяющие структуру проекта (Connect, Disconnect, Add, Remove элементов дерева), пока система находится в режиме исполнения. Любая попытка вызвать такие методы во время runtime приводит к исключению:

```
Изменение проекта в режиме исполнения запрещено.
```

## Причина

Защита реализована внутри `MasterSCADA.Hlp.ITreePinHlp` (декомпиляция, `MasterSCADA.Common.dll`). Перед каждой операцией модификации проверяется флаг `IProjectHlp.InRuntime`:

```csharp
public virtual void Connect(ITreePinHlp pin)
{
    if (this.Project.InRuntime)
        throw new Exception("Изменение проекта в режиме исполнения запрещено.");
    // ...
}
```

То же самое происходит во всех overload'ах `Connect` / `Disconnect` и в операциях добавления/удаления узлов.

## Shutdown-последовательность платформы

Снятие флага `_inRuntime = false` происходит **позже**, чем фактический выход FB из runtime-цикла. Полная последовательность завершения:

1. `rrsAfterStopThreads` — `RTCycles.Stop()` останавливает потоки исполнения. Может показывать модальный `ShutdownForm`, который качает оконные сообщения.
2. Native COM-host вызывает `put_DesignMode(1)` на каждом FB → срабатывает `ToDesign()`.
3. `rrsDoneRuntime` — финальные cleanup-скрипты.
4. `RTManager.Done()` → `IProjectHlp.DoneRuntime()` → `_inRuntime = false`.

То есть в момент срабатывания `ToDesign()` (шаг 2) флаг `InRuntime` всё ещё `true`. Любой синхронный вызов модифицирующих методов на этом шаге всё равно бросит исключение.

## Workaround: отложенное выполнение

FB, которому нужно изменить дерево, должен:

1. **Накапливать** запланированные операции во время runtime (в `UpdateData()`), сохраняя план в объекте-сервисе на heap.
2. **Из `ToDesign()` запостить отложенный делегат**, который не выполняется немедленно, а ждёт, пока `InRuntime` действительно станет `false`.
3. **Делегат проверяет `IProjectHlp.InRuntime`** перед каждой попыткой выполнения. Если флаг ещё `true`, он перепланирует себя и возвращается. Простой `BeginInvoke` для перепланирования не подходит — см. [`08-begininvoke-reposting-fails.md`](08-begininvoke-reposting-fails.md).
4. **Результат пишется в файл-лог**, а не в поля FB или output-пины — см. [`07-fb-instance-replacement.md`](07-fb-instance-replacement.md).

Принципиальная схема:

```
ToRuntime()  → создать сервис, получить IProjectHlp
UpdateData() → детектировать триггер, собрать план, сохранить в сервисе
ToDesign()   → запостить отложенный делегат через timer, затем cleanup
delegate     → если InRuntime == true: перепланировать; иначе выполнить план
```

## Чего нельзя делать

- Вызывать `Connect`/`Disconnect`/`Add`/`Remove` напрямую из `UpdateData()` или `ToDesign()` — это всегда падение в runtime.
- Полагаться на то, что `ToDesign()` означает «уже не в runtime» — формально это так, но `InRuntime` снимается **после** `ToDesign()`.

## Ссылки

- Реализация workaround: `NtoLib/LinkSwitcher/LinkSwitcherFB.cs`, `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Декомпиляция: `MasterSCADA.Common/MasterSCADA/Hlp/ITreePinHlp.cs` строки 842-866 (см. `MasterScada3Wiki`)
- Связанные проблемы: [07](07-fb-instance-replacement.md), [08](08-begininvoke-reposting-fails.md)
