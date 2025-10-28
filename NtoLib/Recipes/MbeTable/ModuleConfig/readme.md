# Конфигурация модуля рецептов: формат YAML, порядок загрузки и правила валидации

Важно:
- Используется snake_case в YAML (например, `property_type_id`, `min_width`).
- Неизвестные/лишние поля в YAML игнорируются (не вызывают ошибку).
- Все проверки выполняются регистронезависимо.

## Состав конфигурации и порядок загрузки

Модуль ожидает четыре файла конфигурации (имена файлов задаются вызывающей стороной и не зашиты жёстко):
1. `PropertyDefs.yaml`
2. `ColumnDefs.yaml`
3. `PinGroupDefs.yaml`
4. `ActionsDefs.yaml`

Порядок загрузки и проверки:
1. Загружается и валидируется `PropertyDefs.yaml`.
2. Загружается и валидируется `ColumnDefs.yaml`, затем проверяются ссылки на типы из `PropertyDefs.yaml`.
3. Загружается и валидируется `PinGroupDefs.yaml`.
4. Загружается и валидируется `ActionsDefs.yaml`, затем проверяются ссылки на колонки/типы/группы пинов из ранее загруженных файлов и соответствие `default_value` ограничениям типов.

Любая ошибка на любом этапе останавливает процесс загрузки.

На успешной загрузке формируется объект конфигурации, который используется далее в приложении на протяжении всего времени жизни.

Побочные эффекты при использовании фасада:
- При отсутствии директории/файлов или при ошибке валидации отображаются окна MessageBox и выбрасывается `InvalidOperationException` (загрузка прерывается).

## Общие правила парсинга YAML

- Неизвестные поля игнорируются (не приводят к ошибке).
- Каждая секция должна содержать хотя бы один элемент (пустые списки запрещены).

## PropertyDefs.yaml — словарь типов свойств

Назначение: описывает типы данных и их ограничения/форматирование. Идентификаторы из этого файла используются в `ColumnDefs.yaml` и `ActionsDefs.yaml`.

Поддерживаемые системные типы:
- Строка: `System.String`
- Целое 16‑бит: `System.Int16` (см. [документацию](https://learn.microsoft.com/en-us/dotnet/api/system.int16?view=netframework-4.8.1))
- Число с плавающей точкой: `System.Single` (см. [документацию](https://learn.microsoft.com/en-us/dotnet/api/system.single?view=netframework-4.8.1))

Специальные типы по `property_type_id`:
- `Time`
- `Enum`

Для специальных типов:
- Формат `TimeHms` разрешён только для `property_type_id: Time`.

Поддерживаемые форматы отображения (`format_kind`):
- `Numeric` (значение по умолчанию)
- `Scientific` (разрешён только при числовом `system_type`: short/float)
- `TimeHms` (только для `property_type_id: Time`)
- `Int` (для `System.Single` - отображается и сохраняется с отброшеной дробной частью) - добавлен для итераций цикла For

Ограничения:
- `property_type_id`: обязательно, уникально (регистронезависимо).
- `system_type`: обязательно.
- Для нестандартных значений `format_kind` будет ошибка.
- Для `Scientific` требуется числовой `system_type`.
- Для `TimeHms` требуется `property_type_id: Time`.

Числовые границы:
- `min`/`max` применяются к числовым типам (включительно).
- Для строк: `max_length` ограничивает длину.

Пример:
```yaml
- property_type_id: "Temp"
  system_type: "System.Single"
  units: "°C"
  min: 0
  max: 2000
  format_kind: "Numeric"

- property_type_id: "String"
  system_type: "System.String"
  units: ""
  max_length: 255
  format_kind: "Numeric"

- property_type_id: "Time"
  system_type: "System.Int16"
  units: "s"
  format_kind: "TimeHms"

- property_type_id: "Enum"
  system_type: "System.Int16"
  units: ""
  format_kind: "Numeric"
```

Примечание:
- Поле `units` допускает любое строковое значение и не влияет на валидацию.

## ColumnDefs.yaml — структура таблицы рецептов

Назначение: описывает колонки таблицы, в т.ч. тип данных, поведение и UI‑параметры.

Обязательные колонки по ключу (`key`):
- `action`
- `task` (добавлен для количества итераций For)
- `step_duration`
- `step_start_time`
- `comment`

Ограничения (валидация):
- Секция не пуста.
- Ключи колонок (`key`) уникальны (регистронезависимо).
- `business_logic.property_type_id` — обязателен и должен существовать в `PropertyDefs.yaml`.
- `ui.column_type` (если задан) должен быть одним из:
    - `action_combo_box`
    - `action_target_combo_box`
    - `property_field`
    - `step_start_time_field`
    - `text_field`
- `ui.width` — > 2 или ровно `-1`. Значение `-1` разрешено только для колонки `comment`.
- `ui.min_width` — > 2.
- `ui.max_dropdown_items` — > 0.
- Если `ui.column_type: action_combo_box`, то `key` должен быть строго `action`.

UI‑параметры и значения по умолчанию:
- `ui.max_dropdown_items`: по умолчанию `30`.
- `ui.width`: по умолчанию `130`.
- `ui.min_width`: по умолчанию `50`.
- `ui.alignment`: по умолчанию `16` (см. [DataGridViewContentAlignment](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datagridviewcontentalignment?view=netframework-4.8.1)).

Прочее:
- `business_logic.read_only`: булево, делает колонку недоступной для редактирования.
- `business_logic.plc_mapping`: опционально. Формат:
    - `area`: строка (например, `"Int"`, `"Float"`).
    - `index`: целое смещение.
- `business_logic.calculation`: поле допускается, но на текущей версии не используется при сборке конфигурации (не влияет на поведение).

Пример:
```yaml
- key: "initial_value"
  business_logic:
    property_type_id: "Temp"
    read_only: false
    plc_mapping:
      area: "Float"
      index: 0
  ui:
    code: "initial_value"
    ui_name: "Начальное значение"
    column_type: "property_field"
    max_dropdown_items: 10
    width: 160
    min_width: 60
    alignment: "MiddleLeft"

- key: "comment"
  business_logic:
    property_type_id: "String"
    read_only: false
  ui:
    code: "comment"
    ui_name: "Комментарий"
    column_type: "text_field"
    width: -1
    min_width: 50

- key: "action"
  business_logic:
    property_type_id: "Enum"
  ui:
    code: "action"
    ui_name: "Действие"
    column_type: "action_combo_box"
```

## PinGroupDefs.yaml — группы пинов

Назначение: описывает наборы пинов оборудования. Имена групп используются в `ActionsDefs.yaml` для колонок с `property_type_id: "Enum"`.

Ограничения (валидация):
- Секция не пуста.
- `group_name`: обязательно, уникально (регистронезависимо).
- `pin_group_id`: обязателен, > 0, уникален.
- `first_pin_id`: обязателен, > 0.
- `pin_quantity`: обязателен, > 0.
- Порядок идентификаторов: `first_pin_id` должен быть >= `pin_group_id`.
- Диапазоны пинов не должны пересекаться между группами:
    - Диапазон вычисляется как `[first_pin_id .. first_pin_id + pin_quantity - 1]`.

Пример:
```yaml
- group_name: "Valves"
  pin_group_id: 450
  first_pin_id: 451
  pin_quantity: 32

- group_name: "TempSensors"
  pin_group_id: 400
  first_pin_id: 401
  pin_quantity: 16
```

## ActionsDefs.yaml — справочник действий

Назначение: определяет доступные действия (команды), их длительность и набор задействованных колонок с типами и, при необходимости, значениями по умолчанию.

ID действий, зарезервированные системой: 10 - `Wait`, 20 - `For`, 30 - `EndFor`, 40 - `Pause`. Из конфигурации можно удалить эти действия, но нельзя создавать свои действия отличного назначения с тем же ID.

Ограничения (валидация):
- Секция не пуста.
- `id`: обязателен (> 0), уникален.
- `name`: обязателен.
- `deploy_duration`: обязателен; допустимые значения:
    - `Immediate`
    - `LongLasting`
- `columns`: обязателен (список).
- Для каждой записи в `columns`:
    - `key`: обязателен, должен существовать в `ColumnDefs.yaml`.
    - `property_type_id`: обязателен, должен существовать в `PropertyDefs.yaml`.
    - `group_name`: обязателен, если `property_type_id == "Enum"`; значение должно существовать в `PinGroupDefs.yaml`.
    - Ключи колонок внутри одного действия — уникальны (регистронезависимо).
- Для действий с `deploy_duration: LongLasting` обязательно наличие колонки с `key: "step_duration"`.

Проверка `default_value`:
- Можно указывать `default_value` (строка) для колонки действия.
- Если тип из `PropertyDefs.yaml` — строковый (`System.String`), проверяется соответствие `max_length` (если задан).
- Если тип — числовой (`System.Int16` или `System.Single`), значение должно парситься и попадать в диапазон `min`/`max` (если заданы).
- Для `property_type_id: "Enum"` и `property_type_id: "Time"` проверка значения по умолчанию не выполняется.
- Для `Enum` значением по умолчанию берется действие с минимальным id.
- Нельзя задавать `default_value` для колонки, которая отмечена как `read_only` в `ColumnDefs.yaml` — это ошибка.

Пример:
```yaml
- id: 10
  name: "Wait"
  deploy_duration: "LongLasting"
  columns:
    - key: "step_duration"
      property_type_id: "Time"
      default_value: "10"
    - key: "comment"
      property_type_id: "String"

- id: 1100
  name: "OpenValve"
  deploy_duration: "Immediate"
  columns:
    - key: "channel"
      property_type_id: "Enum"
      group_name: "Valves"
    - key: "comment"
      property_type_id: "String"
```

## Загрузка конфигурации из приложения

- Перед загрузкой проверяется наличие директории и каждого файла (по переданным именам).
- При отсутствии директории или файлов показывается окно MessageBox с перечнем ошибок и выбрасывается `InvalidOperationException`.
- При ошибках валидации (структура, ссылки, значения по умолчанию и т. п.) показывается окно MessageBox с агрегированным списком ошибок и выбрасывается `InvalidOperationException`.

Поведение парсера YAML:
- Пустой файл — ошибка.
- Ошибка парсинга YAML — ошибка.
- Лишние поля в YAML игнорируются.

## Ошибки и коды ошибок

Коды ошибок (в metadata поля `code`) и условия возникновения:

- `ConfigFileNotFound`
    - Файл конфигурации не найден по указанному пути.
- `ConfigParseError`
    - Ошибка чтения файла (I/O) или ошибка десериализации YAML.
- `ConfigInvalidSchema`
    - Пустой путь к файлу.
    - Пустое содержимое файла/секции.
    - Нарушены структурные требования к полям (пустые обязательные поля, неверные значения `format_kind`, недопустимые `width`/`min_width`/`max_dropdown_items`, ширина `-1` не для `comment`, неизвестный/недопустимый `deploy_duration`, пересечение диапазонов пинов, неверный порядок `first_pin_id`/`pin_group_id`, повторение уникальных значений и т. п.).
    - `default_value` не парсится/не попадает в диапазон для числовых типов, превышает `max_length` для строкового типа.
    - Указан `default_value` для колонки, помеченной `read_only`.
- `ConfigMissingReference`
    - Ссылка на несуществующий идентификатор:
        - `business_logic.property_type_id` в колонке отсутствует в `PropertyDefs.yaml`.
        - `columns.key` в действии отсутствует в `ColumnDefs.yaml`.
        - `columns.property_type_id` в действии отсутствует в `PropertyDefs.yaml`.
        - `group_name` в действии отсутствует в `PinGroupDefs.yaml` (когда обязателен для `Enum`).

Примеры сообщений (фактические шаблоны):
- "File path cannot be empty."
- "Configuration file not found at: '...'"
- "Failed to read file '...': ..."
- "...: Collection is empty or null."
- "...: Field '...' is empty or missing."
- "...: Unsupported column types: '...' ('...'), ..."
- "...: column_type 'action_combo_box' can only be used with key='action'."
- "PinGroupDefs.yaml: Pin ranges overlap between '...' [...] and '...' [...]."
- "...: default_value ... exceeds max ..."
- "...: Cannot set default_value for read_only column."