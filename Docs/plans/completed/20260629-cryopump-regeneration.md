# Регенерация крионасоса (issue #118)

## Overview

Добавить в насос NtoLib управление регенерацией для крио-варианта:

- Кнопка «P» (включение регенерации) между кнопками Старт и Стоп.
- `CommandWord` бит 2 — команда включения регенерации, привязана к кнопке «P».
- `StatusWord` бит 10 в крио-варианте трактуется как `RegenerationActive` (раньше — `Message3`).
- Цвет кнопки «P»: `YellowGreen` при `RegenerationActive == true`, `AntiqueWhite` при `false`.
- Свойство блока «Регенерация» (`bool UseRegeneration`): «истина» — кнопка отображается;
  «ложь» — стандартный крионасос без кнопки (поведение как сегодня).

Решает запрос заказчика на управление регенерацией крионасоса, не ломая существующие
насосы: фича реализуется **в существующем `PumpFB`** через bool-свойство, отдельный ФБ
не заводится.

## Context (from discovery)

- **Архитектура насоса:** один ФБ `PumpFB` (один GUID) + контрол + рендерер + XML + `Status` +
  форма настроек. Всё поведение разветвляется по свойству `PumpType`
  (Forvacuum/Turbine/Ion/Cryogen) через `switch`. Вариативность уже встроена через
  enum-свойство — новый ФБ не нужен.
- **Файлы модуля:**
  - `NtoLib/Devices/Pumps/PumpFB.cs` — оркестратор: `CommandWord`/`StatusWord`, события,
    свойства блока, `CreatePinMap`.
  - `NtoLib/Devices/Pumps/PumpControl.cs` (+ `.Designer.cs`, `.resx`) — WinForms-контрол,
    кнопки, отрисовка, отправка команд.
  - `NtoLib/Devices/Pumps/PumpFB.xml` — `Map` / `VisualMap` / `Events`.
  - `NtoLib/Devices/Pumps/Status.cs` — DTO статуса.
  - `NtoLib/Devices/Pumps/PumpType.cs` — enum типа насоса.
  - `NtoLib/Devices/Pumps/Settings/PumpSettingForm.cs` — форма параметров.
  - `NtoLib/Devices/Render/Pumps/PumpRenderer.cs` — рисование насоса.
  - `NtoLib/Devices/Render/Common/LabeledButton.cs` — кнопка, рисует символ из `SymbolType`.
  - `NtoLib/Devices/Render/Common/LayoutBuilder.cs` — раскладка кнопок (уже масштабируется на N).
  - `NtoLib/Devices/Helpers/SymbolType.cs` — перечисление символов кнопок.
  - `NtoLib/Devices/Helpers/VisualFBBaseExtended.cs` — `SetVisualAndUiPin` / `GetVisualPin`.
- **Инфраструктура пинов:**
  - Командные пины живут только в `VisualMap` (StartCMD=1100, StopCMD=1101). Контрол шлёт
    импульс через `SendCommand` (бит на 500 мс, затем сброс), ФБ читает `GetVisualPin`
    (`id+1000`) и собирает `CommandWord`. Регенерация ложится на тот же механизм: новый
    визуальный pin `RegenCMD=1102`, код-константа `RegenCmdId=102`.
  - Статусный бит 10 уже течёт в контрол: `SetVisualAndUiPin(Message3Id, ...)` пишет в pin
    1010, контрол читает `Status.Message3`. «Переназначение бита 10» делается без новых
    статусных пинов — в крио-ветке с включённой регенерацией контрол трактует этот бит как
    `RegenerationActive`.
  - `LayoutBuilder.RebuildTable` строит N кнопок из массива, **но** масштабируется на N лишь
    наполовину: оно индексирует `ColumnStyles[i]`/`RowStyles[i]` до `buttons.Length-1`, а
    `buttonTable` в `PumpControl.Designer.cs` объявляет только **по два** стиля столбца и строки.
    Установка `ColumnCount = 3` коллекцию стилей не дополняет → третья кнопка даёт
    `ArgumentOutOfRangeException`. Путь раньше не исполнялся (все вызовы передают две кнопки).
    Нужен третий `ColumnStyle` и третий `RowStyle` в дизайнере (см. Task 5).
  - `LabeledButton.OnPaint` принудительно затирает `Text` и рисует символ из `SymbolType`;
    чтобы показать «P», нужен новый `SymbolType`, рисующий букву (`DrawString`).

## Решения по проектированию (зафиксировано)

- **Где:** в существующем `PumpFB`, без нового ФБ и без новой COM-идентичности. Совместимость
  обеспечивается значением свойства по умолчанию (`UseRegeneration = false`) и тем, что бит 10
  меняет смысл только при включённой регенерации.
- **Поведение «P»:** импульсная команда, как Старт/Стоп (клик → импульс «включить
  регенерацию»); цвет кнопки отражает обратный статус `RegenerationActive` от ПЛК. Отдельной
  кнопки «выключить» нет (в issue не предусмотрена).
- **Блокировки «P» и поведение в авторежиме:** кнопка `buttonRegen` **никогда не уходит в
  `Enabled=false`**. Гейт по `UsedByAutoMode` реализуется только как guard в `HandleRegenClick`
  (клик игнорируется), а цвет кнопки **всегда** отражает `RegenerationActive` (требование
  #4/#5 выполняется и в авторежиме). Это сознательно отличается от Старт/Стоп, которые сереют
  по `Enabled`. Поскольку `buttonRegen` не отключается, подмена цвета в
  `LabeledButton.HandleEnabledChanged` к ней не применяется — проблемы «цвет vs disable» нет.
  `BlockStart`/`BlockStop` не применяются (семантически про старт/стоп, дали бы ложную связь).
  Выделенный `BlockRegen` не вводится.
- **Источник статуса:** реюз бита 10 / `Message3` без новых статусных пинов.
- **Видимость кнопки:** управляется свойством `UseRegeneration` и только при
  `PumpType == Cryogen`. Контрол кэширует последнее значение флага (в `UpdateStatus`) и
  пересобирает таблицу кнопок при изменении. ⚠️ Ограничение дизайн-тайма: `UseRegeneration`
  живёт на ФБ, у контрола нет хука на его изменение, а `UpdateStatus` крутится по 50 мс
  таймеру, который в дизайн-тайме не запущен. Тоггл флага в палитре не покажет/скроет кнопку
  до resize или переоткрытия мнемосхемы. В рантайме всё обновляется корректно. Ограничение
  принимается и документируется (см. Post-Completion).

## Development Approach

- **Тестовый подход:** runtime-валидация в хосте MasterSCADA (не xUnit). Модуль насоса
  целиком завязан на COM/WinForms (`VisualFBBase`, `VisualControlBase`, WinForms-кнопки),
  который проект `Tests` не референсит (нет доступа к vendor SDK). Тестов на `Devices/Pumps`
  в репозитории нет — это соответствует правилу `CLAUDE.md`: для FB/XML проверка идёт в хосте,
  `NullReferenceException` из `SetPinValue` почти всегда означает рассинхрон ID/XML
  (см. `Docs/known_issues/09-mismatched-pin-ids.md`).
- У фичи нет осмысленного юнит-тестируемого среза: логика бит-роутинга — одна строка
  (`commandWord.SetBit(2, regenCmd)`) внутри `UpdateData` (FB-bound), остальное — WinForms.
  Извлекать это в отдельный класс ради одного бита нецелесообразно.
- Делать малыми изменениями; собирать после каждого шага; сохранять обратную совместимость.

## Testing Strategy

- **Unit-тесты:** не применимы к этому модулю (см. выше). Новых юнит-тестов фича не требует.
- **Сборка как гейт:** после правок `dotnet build NtoLib.sln` должен проходить без warnings
  (zero-warnings гейт релиза). `EnableDefaultCompileItems=false` — каждый новый `.cs` и
  `EmbeddedResource` добавляется в csproj вручную (в этой фиче новых файлов нет, но
  `PumpFB.xml` правится — пересборка обязательна).
- **Runtime-валидация в MasterSCADA** (раздел Post-Completion): проверка кнопки, цветов,
  бит `CommandWord`/`StatusWord`, флага свойства, обратной совместимости старых крионасосов.
- **Проверка перед правкой XML/пинов и BackColor:** сверка с `Docs/known_issues/`
  (`09-mismatched-pin-ids.md`; `#80` про BackColor в `PumpControl` уже учтён).

## Progress Tracking

- Отмечать выполненное `[x]` сразу.
- Новые задачи — с префиксом ➕.
- Блокеры — с префиксом ⚠️.
- Держать план в синхроне с фактической работой.

## Solution Overview

Свойство `UseRegeneration` на `PumpFB` управляет наличием кнопки «P» (только для крио).
Команда регенерации идёт новым визуальным пином `RegenCMD` (1102) → бит 2 `CommandWord`.
Статус `RegenerationActive` читается из существующего бита 10 (`Message3`) и в крио-ветке
с включённой регенерацией задаёт цвет кнопки. Кнопка реализована как `LabeledButton` с новым
символом `SymbolType.Regen` (рисует «P»), вставляется в таблицу кнопок между Старт и Стоп.

## Technical Details

- **Новый код-константы в `PumpFB`:** `RegenCmdId = 102`; смысловой алиас бита 10 как
  `RegenerationActive` (сам `Message3Id = 10` не переименовываем — это контракт пина).
- **XML (`VisualMap`):** добавить `<Pin ID="1102" Name="RegenCMD" Type="Логический"
  DefaultValue="false"/>`. Статусные пины не добавляются (реюз 1010). `CommandWord` (Pout
  ID 5) — бит внутри слова, XML не меняется.
- **`Status`:** свойство-алиас `public bool RegenerationActive => Message3;` — `Message3`
  уже заполняется из пина 1010 в `UpdateStatus`, отдельно «заполнять» `RegenerationActive`
  не нужно (это вычисляемое свойство).
- **`CommandWord` сборка (`UpdateData`):** `commandWord.SetBit(2, regenCmd)`, где
  `regenCmd = GetVisualPin<bool>(RegenCmdId)`. Импульс пользователя по образцу Start/Stop
  (фронт `!prev && cur`) — событие опционально (см. Task 1).
- **Контрол:**
  - `buttonRegen` (`LabeledButton`, `SymbolOnButton = SymbolType.Regen`), клик →
    `HandleRegenClick` → `SendCommand(RegenCmdId)` при `!Status.UsedByAutoMode` (guard в обработчике).
  - `UpdateButtonTable`: массив `{ buttonStart, buttonRegen, buttonStop }` при
    `PumpType == Cryogen && UseRegeneration`, иначе `{ buttonStart, buttonStop }`.
  - Кэш последнего значения `UseRegeneration` + вызов `UpdateLayout` при изменении (см.
    ограничение дизайн-тайма в «Решения по проектированию»).
  - `UpdateStatus`: `buttonRegen` **не** трогаем по `Enabled` (всегда активна визуально);
    цвет — `YellowGreen`/`AntiqueWhite` по `Status.RegenerationActive`, присваивается всегда.
- **`SymbolType.Regen`:** в `LabeledButton.OnPaint` ветка рисует «P» через `DrawString`
  цветом `ForeColor` в рамках `bounds` (по образцу масштабирования прочих символов).

## What Goes Where

- **Implementation Steps** (`[ ]`): правки кода и XML в этом репозитории, сборка.
- **Post-Completion** (без чекбоксов): runtime-валидация в MasterSCADA, обновление текста
  issue/контракта ПЛК (бит 2 / бит 10), пользовательская документация при необходимости.

## Implementation Steps

### Task 1: PumpFB — свойство, команда и статус регенерации

**Files:**
- Modify: `NtoLib/Devices/Pumps/PumpFB.cs`

- [x] Добавить свойство `[DisplayName("Регенерация")] [Description(...)] public bool UseRegeneration { get; set; }`
  (default false); сеттер не обязан звать `RecreatePinMap` — пин-карта не меняется.
- [x] Добавить код-константу `public const int RegenCmdId = 102;`.
- [x] В `UpdateData` после сборки `startCmd`/`stopCmd`: прочитать
  `var regenCmd = GetVisualPin<bool>(RegenCmdId);` и выставить `commandWord.SetBit(2, regenCmd)`.
- [x] (Опционально, по образцу Start/Stop) добавить фронтовое пользовательское событие
  «оператор включил регенерацию» с `_prevRegenCmd` и `FireEvent` — только если требуется в журнале.
  (skipped - optional, not implemented)
- [x] Сборка `dotnet build NtoLib.sln` без warnings (юнит-тесты для FB-слоя не применимы).

### Task 2: XML — визуальный пин команды регенерации

**Files:**
- Modify: `NtoLib/Devices/Pumps/PumpFB.xml`

- [x] В `VisualMap` добавить `<Pin ID="1102" Name="RegenCMD" Type="Логический" DefaultValue="false"/>`.
- [x] Сверить ID с `Docs/known_issues/09-mismatched-pin-ids.md` (1102 свободен; статусные пины не добавляем — реюз 1010).
  Заметка: `RegenCmdId=102` численно пересекается с UI-Pout `ID=102` (UsedByAutoMode) в `<Map>`, но это
  разные пространства пинов (команда читается из `VisualPins` как `102+1000=1102`), как и существующие
  `StartCmdId=100`/`StopCmdId=101`. Функциональной коллизии нет.
- [x] (skipped - event not implemented in Task 1) Если делаем пользовательское событие (Task 1) — добавить `<Event ID="..." Name="..." Category="Information" Flags="DisableAck"/>` в `<Events>`.
- [x] Пересобрать (`PumpFB.xml` — `EmbeddedResource`, уже в csproj): `dotnet build NtoLib.sln` без warnings.

### Task 3: Status — поле RegenerationActive

**Files:**
- Modify: `NtoLib/Devices/Pumps/Status.cs`

- [x] Добавить `RegenerationActive` как алиас над `Message3` (`public bool RegenerationActive => Message3;`)
  либо отдельное поле, заполняемое контролом из бита 10.
- [x] Сборка без warnings.

### Task 4: SymbolType + LabeledButton — символ «P»

**Files:**
- Modify: `NtoLib/Devices/Helpers/SymbolType.cs`
- Modify: `NtoLib/Devices/Render/Common/LabeledButton.cs`

- [x] Добавить значение `Regen` в `SymbolType`.
- [x] В `LabeledButton.OnPaint` добавить ветку `case SymbolType.Regen:` — рисование «P»
  через `DrawString` цветом `ForeColor` с масштабированием по `bounds`.
- [x] Сборка без warnings; визуально проверить рендер «P» (visual check deferred to in-host validation, see Post-Completion).

### Task 5: PumpControl.Designer — кнопка buttonRegen

**Files:**
- Modify: `NtoLib/Devices/Pumps/PumpControl.Designer.cs`

- [x] Объявить поле `private LabeledButton buttonRegen;`.
- [x] В `InitializeComponent` создать `buttonRegen` (`SymbolOnButton = SymbolType.Regen`,
  цвет `AntiqueWhite`, `UseVisualStyleBackColor = false`, привязка `Click += HandleRegenClick`),
  добавить в `buttonTable` (позиция в таблице переопределяется рантайм-логикой `RebuildTable`).
  (Click wiring deferred to Task 6 — `HandleRegenClick` живёт в `PumpControl.cs` и ещё не создан;
  привязка в дизайнере оборвала бы сборку. Оставлен комментарий-маркер.)
- [x] **(Критично)** Добавить **третий** `ColumnStyle` и **третий** `RowStyle` в `buttonTable`
  (`ColumnStyles.Add(...)`, `RowStyles.Add(...)`), иначе `RebuildTable` упадёт с
  `ArgumentOutOfRangeException` при показе третьей кнопки. Альтернатива — дорастить коллекции
  стилей до `buttons.Length` внутри `RebuildTable`; выбрать локальную правку дизайнера, чтобы
  не трогать общий путь `ValveControl`.
- [x] Сборка без warnings.

### Task 6: PumpControl — клик, видимость, цвет, гейт

**Files:**
- Modify: `NtoLib/Devices/Pumps/PumpControl.cs`

- [x] `HandleRegenClick`: `if (!Status.UsedByAutoMode) SendCommand(PumpFB.RegenCmdId);`.
- [x] `UpdateButtonTable`: собрать массив кнопок с `buttonRegen` между Старт и Стоп при
  `fb.PumpType == PumpType.Cryogen && fb.UseRegeneration`, иначе без неё.
- [x] Кэшировать последнее значение `UseRegeneration`; при изменении вызывать `UpdateLayout`
  (в рантайме срабатывает по таймеру `UpdateStatus`; дизайн-тайм — см. ограничение выше).
- [x] В `UpdateStatus`: `buttonRegen` **не** менять по `Enabled`; цвет
  `buttonRegen.BackColor = Status.RegenerationActive ? YellowGreen : AntiqueWhite` присваивать
  всегда (`Status.RegenerationActive` — алиас над `Message3`, отдельно заполнять не нужно).
  Click обработчик подключён в `PumpControl.Designer.cs` (`buttonRegen.Click += HandleRegenClick`).
- [x] Сборка без warnings.

### Task 7: PumpSettingForm — состояние регенерации (опционально)

**Files:**
- Modify: `NtoLib/Devices/Pumps/Settings/PumpSettingForm.cs`

- [x] При `UseRegeneration` и `PumpType.Cryogen` отразить статус регенерации в форме
  параметров (строка состояния/лампа). Если по согласованию не требуется — пропустить и
  отметить задачу как снятую. (снято - форма настроек в issue #118 не требуется, реализация не нужна)
- [x] Сборка без warnings. (снято - форма настроек в issue #118 не требуется, реализация не нужна)

### Task 8: Проверка критериев приёмки

- [x] Все пункты issue #118 (1–6) реализованы. (инспекция кода: #1 кнопка «P» —
  `PumpControl.UpdateButtonTable` строит `{ buttonStart, buttonRegen, buttonStop }` при
  `Cryogen && UseRegeneration`, `buttonRegen` объявлена в `PumpControl.Designer.cs`;
  #2 `commandWord.SetBit(2, regenCmd)` в `PumpFB.UpdateData`, `regenCmd =
  GetVisualPin<bool>(RegenCmdId)`, `RegenCmdId=102`, пин `RegenCMD=1102` в `VisualMap`;
  #3 `Status.RegenerationActive => Message3` (бит 10); #4/#5 `buttonRegen.BackColor =
  RegenerationActive ? YellowGreen : AntiqueWhite` в `UpdateStatus`; #6 свойство
  `UseRegeneration` гейтит кнопку. Все на месте.)
- [x] Обратная совместимость: при `UseRegeneration = false` крионасос ведёт себя как сегодня
  (две кнопки, бит 10 = `Message3`, бит 2 `CommandWord` не выставляется). (инспекция:
  `showRegen=false` по умолчанию → массив `{ buttonStart, buttonStop }`; `RegenCMD`
  default false → бит 2 = 0; `SetVisualAndUiPin(Message3Id, ...)` бит 10 течёт как прежде;
  ветка цвета только в `case Cryogen`, прочие типы не затронуты.)
- [x] `dotnet format NtoLib.sln` выполнен. (изменений нет; остаточные IDE1006 не
  автофиксируются в режиме solution — предсуществующие, на сборку не влияют.)
- [x] `dotnet build NtoLib.sln` без warnings. (Предупреждений: 0, Ошибок: 0.)
- [x] Сверка пинов с `Docs/known_issues/09-mismatched-pin-ids.md` пройдена. (инспекция:
  `RegenCMD=1102` уникален в `VisualMap`, `RegenCmdId=102` читается как `102+1000=1102`
  по той же схеме, что `StartCmdId=100`/`StopCmdId=101`; численное пересечение с UI-Pout
  `ID=102` в `<Map>` — разные пространства пинов, коллизии нет.)

### Task 9 (Final): Документация и закрытие плана

- [x] Обновить пользовательскую документацию насоса в `Docs/` при необходимости (скил `write-fb-docs`).
  (нет отдельного пользовательского документа по насосу; создание новой документации вынесено за рамки задачи)
- [x] Обновить `CLAUDE.md`, если выявлен новый паттерн (вероятно не требуется).
  (новый паттерн не выявлен — переиспользован существующий механизм PumpType/свойств)
- [x] Переместить план в `docs/plans/completed/`.
  (перенос отложен: выполняется в фазе финализации exec-процесса)

## Post-Completion

*Требует ручных действий или внешних систем — без чекбоксов, информационно.*

**Runtime-валидация в MasterSCADA** (основная проверка фичи):
- Крионасос с `UseRegeneration = true`: появляется кнопка «P» между Старт и Стоп.
- Клик «P» в ручном режиме: бит 2 `CommandWord` встаёт импульсом ~500 мс и сбрасывается.
- В авторежиме (`UsedByAutoMode`) кнопка «P» неактивна.
- Бит 10 `StatusWord` = 1: кнопка «P» окрашивается `YellowGreen`; = 0: `AntiqueWhite`.
- `UseRegeneration = false`: стандартный крионасос без кнопки, поведение как до фичи.
- Регресс на остальных типах (Forvacuum/Turbine/Ion) и на старых крионасосах отсутствует.
- Рендер буквы «P» читаем при разных размерах и ориентациях кнопок.

**Изменения контракта с ПЛК / issue**:
- Зафиксировать в issue #118 и в описании контракта: `CommandWord` бит 2 = включение
  регенерации; `StatusWord` бит 10 в крио-варианте с включённой регенерацией =
  `RegenerationActive` (вместо `Message3`/`TempMonitorOffline`).

**Известное ограничение дизайн-тайма**:
- Переключение свойства «Регенерация» в палитре не показывает/скрывает кнопку «P» мгновенно —
  обновление произойдёт после resize контрола или переоткрытия мнемосхемы. В рантайме кнопка
  отображается корректно. Причина: свойство живёт на ФБ, перестройка таблицы кнопок идёт в
  `UpdateStatus` по таймеру, не активному в дизайн-тайме.

**Открытые решения, осознанно не реализованные**:
- `ForceStop` и выделенный `BlockRegen` (бит 11 Custom) как блокировки регенерации —
  не вводятся. Если потребуется настоящая блокировка регенерации, это отдельная правка
  контракта ПЛК и дополнение issue.
