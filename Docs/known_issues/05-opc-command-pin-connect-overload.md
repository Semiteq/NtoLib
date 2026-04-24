# Восстановление связей с Command-пинами: требуется no-arg `Connect`

## Описание проблемы

При восстановлении связей OPC-группы `PlanExecutor` вызывает `ITreePinHlp.Connect` для каждой записи из `tree.json`. Для связей с «Результат»-пинами Command-блоков MasterSCADA (например, `MBE_Locomotive.Транспорт.CMD.Результат`) явный вызов с `EConnectionType.ctGenericPin` бросает исключение:

```
System.ArgumentOutOfRangeException: Значение не попадает в ожидаемый диапазон.
   at ...ConnectByName(...)
   at NtoLib.OpcTreeManager.TreeOperations.PlanExecutor.TryConnectLink(...)
```

В логе это выглядит так:

```
[ERR] Connect Система.АРМ.OPC UA Siemens.ServerInterfaces.MBE.Action.ControlWord$
      ↔ MBE_Locomotive.Транспорт.CMD.Результат — Значение не попадает в ожидаемый диапазон.
```

Остальные связи в том же поддереве (обычные `directPin`/`directPout`, `iconnect`) подключаются успешно -- падают только эти. В проекте на ~250 связей таких случаев ~24 (все `ControlWord$` и `SelectPosWord$`). Пока такие связи не восстанавливаются, команды на соответствующих каналах не доходят до исполнительных устройств.

## Причина

`MasterSCADA.Hlp.ITreePinHlp` (декомпиляция, `MasterSCADA.Common.dll`) предоставляет **два** overload'а `Connect`:

**Без `EConnectionType` (строки 842-854)** -- автороутинг:

```csharp
public virtual void Connect(ITreePinHlp pin)
{
    if (this.Project.InRuntime) throw ...;
    if (this.ObjectType == EObjectType.otValue && pin.PinType == PinTypes.PT_PIN)
        this.TreePin.ConnectByName(pin.FullName, 1, 0);
    else if (this.PinType == pin.PinType &&
             (this.HostType == HostItemType.hitAttribute ||
              pin.HostType == HostItemType.hitAttribute ||
              this.PinType == PinTypes.PT_POUT))
        ((IConnect) this.TreeObject).Connect((object) pin.TreeObject, 1, 1);
    else if (this.IsPinNotPout)
        pin.TreePin.ConnectByName(this.FullName, 1, 0);
    else
        this.TreePin.ConnectByName(pin.FullName, 1, 0);
}
```

Ключевая ветка -- на строке 848: если обе стороны `PT_POUT` (что верно для пары `CMD.Результат` как Pout командного блока и `ControlWord$` как Pout-половины `PinPout`-пина OPC), Connect роутится через `IConnect.Connect`, то есть **iconnect-путь**.

**С явным `EConnectionType` (строки 856-866)** -- без автороутинга:

```csharp
public virtual void Connect(ITreePinHlp pin, EConnectionType connectionType)
{
    if (this.Project.InRuntime) throw ...;
    if (connectionType == EConnectionType.ctIConnect)
        ((IConnect) this.TreeObject).Connect((object) pin.TreeObject, 1, 1);
    else if (connectionType == EConnectionType.ctGenericPin)
        pin.TreePin.ConnectByName(this.FullName, 1, 0);
    else
        this.TreePin.ConnectByName(pin.FullName, 1, 0);
}
```

При `ctGenericPin`/`ctGenericPout` всегда идёт `ConnectByName`. Для POUT-POUT пар vavobj на уровне COM отклоняет такое соединение с `ArgumentOutOfRangeException` -- Command-блок не принимает direct-связи по спецификации (см. вендорный код в `ITreePinHlp.cs:890` и `ControllerHostObject.cs:120-122`, где Command-пины везде проверяются против `ctIConnect`).

Обнаружить «этот peer требует iconnect» со стороны коллектора непросто: `peer.ObjectType` возвращает `otFBPout` (это Pout FB-блока), а `otCommand` хранится на **родителе** (`peer.GetParent(EParentType.ptFB).ObjectType`). Мы пробовали явную детекцию -- она хрупкая (срабатывает не для всех Command-подобных случаев) и дублирует switch внутри `Connect(pin)`, который уже умеет роутить правильно.

## Исследованные решения

| Подход | Результат |
|--------|-----------|
| Пропускать `$`-сиблинги при сборе (как в вендорной `OpcGroup/example.md`) | Теряет Command-связи целиком -- они существуют только на `$`-стороне |
| Пометка `linkType = iconnect` по `peer.ObjectType == otCommand` в `LinkCollector` | Не срабатывает: ObjectType "Результат"-пина = `otFBPout`, не `otCommand` |
| Пометка по `peer.GetParent(EParentType.ptFB).ObjectType == otCommand` | Потенциально работает, но дублирует вендорный switch и требует поддерживать список «типов-требующих-iconnect» при каждом изменении MasterSCADA |
| Retry `Connect(ctIConnect)` в `catch (ArgumentOutOfRangeException)` | Работает, но лечит симптом, тратит лишний Connect на каждую такую связь и маскирует реальные ошибки с тем же классом исключения |
| **Использовать no-arg `Connect(pin)` и доверить роутинг vavobj** | Работает. Использовано в `NtoLib.LinkSwitcher.TreeOperations.LinkExecutor` для тех же задач |

## Текущий workaround

`PlanExecutor.TryConnectLink` использует no-arg overload для `directPin`/`directPout` -- как LinkSwitcher:

```csharp
if (link.LinkType == LinkTypes.IConnect)
{
    localPin.Connect(externalPin, EConnectionType.ctIConnect);
}
else if (link.LinkType == LinkTypes.DirectPin)
{
    localPin.Connect(externalPin);
}
else if (link.LinkType == LinkTypes.DirectPout)
{
    externalPin.Connect(localPin);
}
```

`linkType` из `tree.json` решает только кто субъект, а кто аргумент -- направление выбирает vavobj. `iconnect` оставлен с явным типом, потому что там автороутинга нет -- iconnect идёт через другой COM-интерфейс (`IConnect.Connect`), и определить его по типу пинов нельзя.

## Чего нельзя делать при правке `PlanExecutor`

- Переписать `directPin`/`directPout` обратно на `Connect(pin, EConnectionType.ctGenericPin/ctGenericPout)` -- сломает Command-пины.
- Пытаться детектировать Command-пины в коллекторе и перелейблить `linkType` -- `peer.ObjectType` не даёт нужного ответа, а проверка родителя дублирует вендорную логику без выгоды. Снапшот хранит `directPin`/`directPout` как есть, роутинг выбирает vavobj.

## Ссылки

- Вендорный источник: `ITreePinHlp.Connect` overloads, `MasterSCADA.Common/MasterSCADA/Hlp/ITreePinHlp.cs` строки 842-866 (декомпиляция в `MasterScada3Wiki`)
- Прецедент в NtoLib: `NtoLib/LinkSwitcher/TreeOperations/LinkExecutor.cs`, метод `ExecuteOperation`
- Код: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`, метод `TryConnectLink`
