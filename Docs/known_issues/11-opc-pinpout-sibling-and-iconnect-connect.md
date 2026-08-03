# iconnect-связи settings-пинов не показывались после перестройки поддерева

История бага: после перестройки OPC-поддерева (`OpcTreeManager`) iconnect-связи
settings-пинов TemperatureControllers (`Kp`, `Ti`, `Td`, `MaxOutput`, `MinOutput`,
`SpeedSP`, `TempOffset`, `PowerOffset`) не показывались подключёнными.

Долговременная механика платформы — модель PinPout `$`-сиблинга, различие
iconnect/directPin и таблица роутинга connect-API — вынесена в
[`../architecture/masterscada-fb-primer.md`](../architecture/masterscada-fb-primer.md)
(раздел про пин-систему): это «как устроена пин-система», а не баг. Здесь остаётся
разбор самого бага.

## Симптом

Settings-пин выходит в дерево двумя связями (см. модель `$`-сиблинга в primer):
iconnect обратной связи на базовом пине и directPin входа на `$`-сиблинге, оба к
одному внешнему элементу. После перестройки поддерева пин показывался
«неподключённым» — в диалоге «Список связей» у него не хватало строки, хотя часть
связей восстанавливалась.

## Реальная первопричина: наша свёртка DedupByWire (баг NtoLib, не платформы)

Первопричина была **на стороне захвата**, не подключения, и это была **наша**
ошибка, а не ограничение платформы. Снапшот терял directPin-половину (input)
каждого settings iconnect-пина: dedup-свёртка (`LinkCollector.DedupByWire`)
складывала пару `(iconnect base, $ directPin)` в одну строку, оставляя iconnect и
отбрасывая directPin. Поэтому восстановление всегда было неполным, а пин не
показывался подключённым.

Исправление — хранить обе половины пина отдельными строками снапшота (точная
тройка `(local, external, linkType)`) плюс capture-инвариант в `BuildLinks`
(warning на iconnect-строку без same-external `$`-directPin близнеца).

## In-code read-back слеп после структурного коммита

Отдельная ловушка платформы (не наш баг): после структурного коммита
`GetConnections` возвращает **пусто** для связей, которые в дереве РЕАЛЬНО
подключены. Значит любой ok/fail-вердикт по read-back — бессмысленный.

Судить об успехе только по **дереву** и по **сохранению/перезагрузке проекта**,
никогда по in-code read-back. Доказано на хосте: лог рапортовал `ok=24 fail=190`,
тогда как в дереве SCADA каждая связь была подключена. Именно read-back-слепота
маскировала неполный захват в первых попытках.

## Рабочее восстановление: direct-first, iconnect-last

1. Захватить **обе** половины пина (и iconnect базового, и directPin `$`-сиблинга).
2. При восстановлении подключать сперва **directPin/directPout**, затем **iconnect**
   (direct-first, iconnect-last).

На корректном снапшоте `ObjectForward` (`localPin.Connect(externalPin, ctIConnect)`)
переподключает всё: iconnect ложится на уже-корректный пин — то самое состояние, в
котором всегда работало ручное редактирование связей. На хосте так переподключились
и twinless feedback iconnect'ы `Setpoint` (`TemperatureSP`/`PowerSP`) — отдельный
`DelayConnection+ApplyChange` не понадобился.

## Опровергнутая теория — id-path collision

Гипотеза «id-path collision / duplicate-connection skip / перестроенный пин навсегда
испорчен, лечится свежими id» — **ОПРОВЕРГНУТА**. Не заносить её как платформенный факт.

- Попытка 7 показала: свежие id ничего не поменяли — нативный id-путь `[item+0x68]` это
  **производный handle**, а не наш `AddPinWithID`-id, так что сдвиг нашего id его не трогает.
- Все восемь попыток шли на **бракованных** снапшотах, где отсутствовала половина связей.
  Отсюда и вся посылка «перестроенный пин навсегда corrupt» — это артефакт неполного захвата,
  а не свойство платформы. Как только захвачены обе половины и direct подключён раньше iconnect,
  штатный `ObjectForward` переподключает всё.

История попыток сохранена в `Docs/plans/iconnect-investigation-log.md` (матрица попыток +
раздел RETRACTED). Матрица — исторический материал, не руководство.

## Ссылки

- Механика платформы (модель `$`-сиблинга, iconnect vs directPin, таблица роутинга):
  [`../architecture/masterscada-fb-primer.md`](../architecture/masterscada-fb-primer.md),
  раздел про пин-систему.
- Захват: `NtoLib/OpcTreeManager/TreeOperations/LinkCollector.cs`, `DedupByWire`
  (точная тройка `(local, external, linkType)`; обе половины сохраняются) + capture-инвариант
  в `BuildLinks` (warning на iconnect-строку без same-external `$`-directPin близнеца).
- Восстановление: `OrderProbesDirectFirst` в
  `NtoLib/OpcTreeManager/TreeOperations/ProbeOrdering.cs` (direct-first порядок) +
  `BuildConnectAction` в `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
  (arm iconnect: `localPin.Connect(externalPin, ctIConnect)`).
- Связанная проблема: [05](05-opc-command-pin-connect-overload.md) — no-arg `Connect`
  и роутинг vavobj; [06](06-runtime-tree-modification-forbidden.md) — запрет модификации
  дерева в runtime.
- История расследования: `Docs/plans/iconnect-investigation-log.md`.
