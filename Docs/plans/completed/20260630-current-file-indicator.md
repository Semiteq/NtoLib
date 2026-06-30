# Индикатор текущего открытого файла рецепта (issue #119)

## Overview

Показать оператору, какой файл рецепта сейчас открыт, в блоках **MbeTable** и
**MbeTableEditor**. При параллельной работе с несколькими файлами легко забыть, что где.

- Тонкая метка в самом низу контрола (под рядом кнопок и статусной строкой), шрифт 8–10.
- Текст: файл не загружен → «Несохранённый рецепт»; файл загружен/сохранён →
  «Сейчас открыт: \<полный путь\>».
- Рантайм-онли: путь не переживает перезапуск MasterSCADA; при пересоздании блока
  (перелинковка) сбрасывается в плейсхолдер.

## Context (from discovery)

- **Понятия «текущий файл» в коде нет.** Путь известен только в общем презентере
  `TablePresenter` (`_openDialog.FileName` в `LoadRecipeAsync`, `_saveDialog.FileName` в
  `SaveRecipeAsync`), передаётся в `RecipeOperationService` и отбрасывается; имя мелькает
  разово в статусной строке и затирается.
- **Статусная строка** — один `Label _labelStatus`, обновляется общим механизмом
  `ServiceStatus`: `LabelStatusSink` безопасно пишет в `Label` (маршалинг в UI-поток через
  `InvokeRequired`/`BeginInvoke`, проверка `IsHandleCreated`/`IsDisposed`). Интерфейс
  `IStatusSink` = `Write(message, StatusKind)` + `Clear()`.
- **Тонкие оболочки.** `MbeTable` (`TableControl.*`) и `MbeTableEditor`
  (`MbeTableEditorControl.*`) — почти дословные дубликаты, переиспользующие общее ядро.
  `TablePresenter` общий; конструируется через общий хелпер `TableControlServices` из обеих
  оболочек одинаковым списком аргументов. `DataGridViewAdapter` (`ITableView`) — один на обе.
- **Раскладка низа абсолютная** (`Location`/`Anchor`, без `StatusStrip`). Высота контрола
  ~534; `_labelStatus` и кнопки на `y≈491`, высота 40 (низ ~531). Места «ниже» нет.
  У редактора нет кнопки `_buttonWrite`; ширины `_labelStatus` отличаются (670 vs 727).
- **Совместимость.** Уже размещённые экземпляры на мнемосхемах хранят свой размер. Если
  растить высоту контрола, новая метка уедет за нижний край старых экземпляров до ручного
  ресайза. → высоту **не растим**.
- `.ConfigureAwait(false)` в `LoadRecipeAsync`/`SaveRecipeAsync` → продолжение не на
  UI-потоке; поэтому пишем в метку только через `LabelStatusSink` (он маршалит сам).
- `TablePresenter` сейчас **игнорирует** `Result` от `_app.LoadRecipeAsync`/`SaveRecipeAsync`.

## Решения по проектированию (зафиксировано)

- **Без нового сервиса.** Переиспользуем существующий `LabelStatusSink`. Общий `TablePresenter`
  получает один параметр конструктора `IStatusSink fileSink`. `ITableView` и `DataGridViewAdapter`
  **не трогаем** (SRP/ISP: адаптер таблицы не должен владеть чужой меткой; маршалинг уже решён в
  `LabelStatusSink`).
- **Место конструирования — `CreatePresenter` каждой оболочки** (`TableControl.Lifecycle.cs:305`
  и `MbeTableEditorControl.Lifecycle.cs:268`), где `TablePresenter` уже создаётся 6 аргументами.
  Туда добавляем 7-й: `new LabelStatusSink(_labelFile, _services.ColorScheme)`
  (`_services.ColorScheme` — та же схема, что в `StatusService.AttachLabel`). `TableControlServices.cs`
  **не трогаем** — это набор зависимостей, презентер он не строит.
- **Форматирование текста — один раз в презентере** (DRY). Sink только пишет строку.
- **Запись `StatusKind.None`** → нейтральный фон (`StatusBgColor`); напрямую через sink, мимо
  `StatusFormatter`, чтобы полный путь не обрезался.
- **Плейсхолдер с первого кадра** — дефолтный `Text = "Несохранённый рецепт"` метки в дизайнере
  (на случай, если при `Initialize()` handle метки ещё не создан и запись через sink пропустится),
  и `BackColor = StatusBgColor` метки в дизайнере (нейтральный фон до первой записи).
- **Сброс при перелинковке** — `Initialize()` пишет плейсхолдер (там handle уже есть).
- **Освобождение sink** — хранить `_fileSink` и звать `(_fileSink as IDisposable)?.Dispose()` в
  `TablePresenter.Dispose()` (симметрично статусному sink, который освобождается через
  `StatusService.Detach()`).
- **Длинный путь** — `_labelFile.AutoEllipsis = true`: при нехватке ширины путь усечётся
  многоточием, а полный текст покажет встроенная всплывающая подсказка `Label` при наведении.
- **Раскладка (высоту контрола 534 НЕ растить, ради совместимости со старыми экземплярами).**
  Текущая геометрия обеих оболочек: `_table` `y=3, h=482` (низ 485); `_labelStatus`+кнопки
  `y=491, h=40`. Шаги: уменьшить высоту `_table` (~482 → ~458, низ ~461); поднять ряд
  статус+кнопки (`y=491 → ~467`); поставить `_labelFile` (`y≈509, h≈20`, `Anchor = Bottom|Left|Right`,
  шрифт 8–10) в освободившуюся полосу внутри высоты 534. Точные координаты подбираются по месту,
  без наложений. Делать раздельно в двух дизайнерах (у редактора нет `_buttonWrite`, ширина
  `_labelStatus` 727 vs 670).

## Testing Strategy

- **Unit-тесты неприменимы:** код в COM/WinForms-слое (презентер зависит от
  `OpenFileDialog`/`SaveFileDialog`/`Label`; Tests не покрывает). Форматирование строки —
  тривиальный тернарник, извлекать ради теста нецелесообразно.
- **Гейт:** `dotnet build NtoLib.sln` без warnings (zero-warnings гейт релиза).
- **Runtime-валидация в хосте MasterSCADA** (Post-Completion): метка, плейсхолдер, путь после
  загрузки/сохранения/Save As, чтение из ПЛК → плейсхолдер, сброс при перелинковке, обе
  оболочки, поведение на уже размещённых экземплярах.
- `dotnet format NtoLib.sln` перед сдачей.

## Progress Tracking

- Отмечать `[x]` сразу; новые задачи — `➕`; блокеры — `⚠️`.

## Solution Overview

Презентер (общий) знает путь и результат операции. Он форматирует строку и пишет её через
переданный `IStatusSink fileSink` (это `LabelStatusSink` над меткой конкретной оболочки) в
четырёх точках. Метка `_labelFile` живёт в дизайнере каждой оболочки (единственное
дублирование — разрешённый thin-shell кусок), плейсхолдер задан её дефолтным текстом.

## Technical Details

- **Формат:** `fullPath is null → "Несохранённый рецепт"`, иначе `"Сейчас открыт: " + fullPath`.
- **Точки записи в презентере:**
  - успех `LoadRecipeAsync` → путь;
  - успех `SaveRecipeAsync` → `_saveDialog.FileName` (Save As покрыт);
  - успех `ReceiveRecipeAsync` (из ПЛК) → `null` (плейсхолдер);
  - `Initialize()` → `null` (сброс/seed).
  Все привязки к файлу — только при `result.IsSuccess`.
- **Конструктор презентера:** `+ IStatusSink fileSink` (7-й параметр). Поле `_fileSink`. Приватный
  помощник `ShowCurrentFile(string? fullPath)` формирует текст и вызывает
  `_fileSink.Write(text, StatusKind.None)`. В `Dispose()` — `(_fileSink as IDisposable)?.Dispose()`.
- **Тип результата:** возврат `Task<Result>` (FluentResults) подтверждён
  (`RecipeOperationService.cs:158/204/265`) — использовать `.IsSuccess`.
- **Конструирование sink:** только в двух `CreatePresenter`; `TableControlServices` не участвует.
- **AutoEllipsis:** длинный путь усекается многоточием со встроенной подсказкой (отдельный
  `ToolTip`-компонент не нужен).

## Implementation Steps

### Task 1: Метки `_labelFile` в обоих дизайнерах (раскладка)

**Files:**
- Modify: `NtoLib/Recipes/MbeTable/TableControl.Designer.cs`
- Modify: `NtoLib/Recipes/MbeTableEditor/MbeTableEditorControl.Designer.cs`

Только дизайнеры — сборка остаётся зелёной (поле метки ни на что не ссылается).

- [x] В `TableControl.Designer.cs`: добавить поле `Label _labelFile`; `Font` 8–10; `Anchor = Bottom|Left|Right`;
  `Text = "Несохранённый рецепт"`; `BackColor = StatusBgColor`-эквивалент (нейтральный фон); `AutoEllipsis = true`.
- [x] Там же — раскладка без роста высоты 534: уменьшить `_table.Size.Height` (~482 → ~458);
  поднять `_labelStatus`+кнопки (`y=491 → ~467`); поставить `_labelFile` (`y≈509, h≈20`) в самый низ; без наложений.
- [x] В `MbeTableEditorControl.Designer.cs`: то же, считая раскладку отдельно (нет `_buttonWrite`, ширина `_labelStatus` 727).
- [x] `dotnet build NtoLib.sln` без warnings (юнит-тесты неприменимы — COM/WinForms-слой).

### Task 2: Логика индикатора (атомарно: презентер + обе точки конструирования)

**Files:**
- Modify: `NtoLib/Recipes/MbeTable/ModulePresentation/TablePresenter.cs`
- Modify: `NtoLib/Recipes/MbeTable/TableControl.Lifecycle.cs`
- Modify: `NtoLib/Recipes/MbeTableEditor/MbeTableEditorControl.Lifecycle.cs`

Одной задачей, чтобы сборка осталась зелёной (изменение сигнатуры конструктора + оба вызова сразу).

- [x] `TablePresenter`: добавить 7-й параметр конструктора `IStatusSink fileSink` (using `...ServiceStatus`), поле `_fileSink`; приватный `ShowCurrentFile(string? fullPath)` (текст по формату → `_fileSink.Write(text, StatusKind.None)`); в `Dispose()` — `(_fileSink as IDisposable)?.Dispose()`.
- [x] Точки записи (только при `result.IsSuccess`): `LoadRecipeAsync` → `ShowCurrentFile(path)`; `SaveRecipeAsync` → `ShowCurrentFile(_saveDialog.FileName)`; `ReceiveRecipeAsync` → `ShowCurrentFile(null)`; `Initialize()` → `ShowCurrentFile(null)` (seed/сброс). Захватывать ранее игнорируемый `Result`.
- [x] `TableControl.Lifecycle.cs` `CreatePresenter` (стр. 305): добавить 7-м аргументом `new LabelStatusSink(_labelFile, _services.ColorScheme)`.
- [x] `MbeTableEditorControl.Lifecycle.cs` `CreatePresenter` (стр. 268): то же с меткой редактора. Никакой логики имени файла в контролах.
- [x] `dotnet build NtoLib.sln` без warnings; всё решение компилируется (сигнатура и оба вызова согласованы).

### Task 3: Проверка критериев приёмки

- [x] Текст и точки записи соответствуют Overview (Load/Save/Save As/Receive/Initialize). (проверено: `TablePresenter.ShowCurrentFile` форматирует «Несохранённый рецепт»/«Сейчас открыт: »+path, `_fileSink.Write(text, StatusKind.None)`; Load→path, Save→`_saveDialog.FileName`, Receive→null, Initialize→null; `_fileSink` освобождается в `Dispose()`)
- [x] Привязка только при `result.IsSuccess`. (проверено: Load стр.87, Save стр.104, Receive стр.124)
- [x] Высота контролов не изменилась (совместимость со старыми экземплярами). (проверено: оба контрола `Size = 968x534`; `_table` h=458 (низ 461); `_labelStatus` y=467 h=40 (низ 507); `_labelFile` y=509 h=20 (низ 529) — без наложений; метка Anchor=Bottom|Left|Right, AutoEllipsis=true, плейсхолдер «Несохранённый рецепт»)
- [x] `dotnet format NtoLib.sln` выполнен. (без изменений; остаются непофиксенные IDE1006 — ожидаемо)
- [x] `dotnet build NtoLib.sln` без warnings. (0 предупреждений, 0 ошибок)
- [x] `dotnet test NtoLib.sln` — существующие тесты не сломаны. (пройдено 293, провалено 0)

### Task 4 (Final): Документация и закрытие плана

**Files:**
- Modify: `Docs/mbe-table.md`
- Modify: `Docs/mbe-table-editor.md`

- [x] Добавить короткий абзац про индикатор текущего файла (внизу контрола; «Несохранённый рецепт» / «Сейчас открыт: …») в обе доки. Соблюсти правило прозы скила `write-fb-docs` (без англицизмов/транслитов/жаргона; идентификаторы — как есть). (добавлено: `Docs/mbe-table.md` §2.7 «Имя текущего рецепта», `Docs/mbe-table-editor.md` §4)
- [x] `CLAUDE.md` — правка не требуется (новый паттерн не вводится; переиспользован `LabelStatusSink`). (не требуется)
- [x] Переместить план в `docs/plans/completed/`. (перенос отложен: выполняется в фазе финализации exec-процесса)

## Post-Completion

*Ручная проверка в хосте MasterSCADA (основная валидация фичи):*
- Плейсхолдер «Несохранённый рецепт» виден сразу при добавлении блока.
- После открытия файла — «Сейчас открыт: \<полный путь\>»; после «Сохранить как» — новый путь.
- Чтение рецепта из ПЛК (только MbeTable) → плейсхолдер.
- Неуспешная загрузка/сохранение не меняет показанный путь.
- Перелинковка/пересоздание блока → плейсхолдер (рантайм-онли, рестарт не переживает).
- Обе оболочки (MbeTable и MbeTableEditor); проверка на уже размещённых на мнемосхемах
  экземплярах — метка видна без ручного ресайза (высота не росла).
- Длинный путь: проверить перенос/обрезание в метке (читаемость на узком блоке).
