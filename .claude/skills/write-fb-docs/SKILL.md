---
name: write-fb-docs
description: "Step-by-step workflow for writing documentation for a Function Block in NtoLib. Covers both headless and visual FBs. Produces a Russian-language markdown doc following the established pattern in Docs/."
---

# Write Documentation for a Function Block

## When to Use

Use this skill when the task involves creating or substantially rewriting documentation for an existing Function Block in `Docs/`. Do NOT use for code changes, known issues, or MbeTable sub-module docs.

## Governing Principle: External Contract Only

The doc describes what the FB does from the outside -- its pins, properties, observable behavior, and user-facing messages. It does NOT describe how the FB is implemented internally.

**Include:** pins, types, defaults, visual properties, user-visible behavior (what happens when the user does X), error messages the user will see, constraints the user must respect (e.g. "wait 20 seconds after stopping Runtime before editing the tree").

**Exclude:** class names, design patterns, internal timers, polling intervals, field names, debounce constants, arbitration logic, serialization details, threading model, control update mechanisms. None of these affect how the user interacts with the FB.

**Threshold for inclusion:** a detail belongs in the doc only if the user must know it to use the FB correctly or to understand behavior they can act on. If a detail is purely internal and the user cannot observe or react to it, omit it.

**When unsure** whether a detail is significant enough to include, ask the user before writing it in. Do not guess -- err on the side of omission.

## Pre-Flight

Before starting, gather this information (read from source code if not provided by the user):

1. **FB type** -- headless (`StaticFBBase`) or visual (`VisualFBBase` / `VisualFBBaseExtended`).
2. **FB class file** -- e.g. `NtoLib/NumericBox/NumericBoxFB.cs`.
3. **XML file** -- e.g. `NtoLib/NumericBox/NumericBoxFB.xml`.
4. **Control file** (visual FBs only) -- e.g. `NtoLib/NumericBox/NumericBoxControl.cs`.
5. **Service/facade files** (headless FBs) -- e.g. `NtoLib/LinkSwitcher/Facade/`.

Read all relevant source files before writing to ensure accuracy -- but remember, the source is for your understanding only. The doc output must contain none of the internal details you read.

## Reference Implementations

Read at least one existing doc of the same FB type before writing:

- **Headless FB docs:** `Docs/config-loader.md`, `Docs/link-switcher.md`
- **Visual FB doc:** `Docs/numeric-box.md`
- **Complex headless FB doc:** `Docs/trend-pens-manager.md`

Mimic their *structure*, not every word: `numeric-box.md`'s TL;DR says "работает через debounce…" and the `readme.md` TOC mentions "арбитрация" — both leak an internal-mechanism term that rule #1 forbids. Do not copy those phrasings; describe observable behavior instead.

## Language and Style Rules

1. **External contract only.** Describe what the FB does, not how it is built. No class names, no pattern names, no internal constants, no architecture. See "Governing Principle" above.
2. **All text in Russian.** Section headers, table content, TL;DR, prose -- everything.
3. **Identifiers keep their original form, never translated:** pin names, enum values, and property names stay in English (`Input`, `Output`, `DisplayFormat`). For the `Тип` column, follow the same-type reference doc: headless FB docs use lowercase English type names (`bool`, `string`, `double`); the visual `numeric-box.md` uses the Russian XML type names (`Вещественный`). Match the existing same-type docs rather than mixing the two.
4. **Prefer name-only pin tables (no DispId/ID column).** Pin IDs are an internal addressing detail; users identify pins by name (this matches `numeric-box.md`). Note: the older headless docs (`config-loader.md`, `link-switcher.md`, `trend-pens-manager.md`) include a leading `| ID |` column — when *rewriting* one of those, keep its existing column shape unless the user asks to modernize it, to avoid a gratuitous reformat.
5. **Concise prose.** Short paragraphs, no filler. Each sentence adds information.
6. **Tables over paragraphs** when listing structured data (pins, properties, errors).
7. **No code blocks** unless showing a tree structure, YAML config, or format example.
8. **File name:** `Docs/<fb-name-lowercase-kebab>.md` (e.g. `numeric-box.md`, `config-loader.md`).

## Document Structure

### Common Sections (both headless and visual)

```markdown
# <EnglishName> -- <Русское описание>

> **TL;DR:** <2-3 предложения: что делает, как работает, ключевые особенности.>

## 1. Интерфейс

### Входы

| Имя | Тип | Описание |
|-----|-----|----------|

### Выходы

| Имя | Тип | Описание |
|-----|-----|----------|

### События (Warnings)              ← only if the FB has events

| Имя | Условие |
|-----|---------|

## N. Логика работы                  ← observable input-output behavior, not internal algorithm

## N. Валидация при вводе            ← what the user sees when input is rejected

## N. Обработка ошибок               ← user-visible error categories and consequences
```

### Additional Sections for Visual FBs

Insert after section 1 (Интерфейс):

```markdown
## 2. Визуальные свойства (окно настроек)

| Свойство | По умолчанию | Описание |
|----------|--------------|----------|
```

If the control has enums exposed as properties (e.g. `DisplayFormat`, `FontSizeMode`), list their values:

```markdown
### <EnumName> (<EnumDisplayName>)

- `Value1` -- описание
- `Value2` -- описание
```

### Additional Sections for Headless FBs

Add if applicable:

```markdown
## N. Параметры (окно настроек)

| Свойство | По умолчанию | Описание |
|----------|--------------|----------|

## N. Логирование                    ← if the FB writes to a log file

- **Путь:** ...
- **Ротация:** ...
- **Содержимое:** ...

## N. Режим исполнения               ← if the FB uses deferred execution
```

### Section Numbering

Number sections sequentially starting from 1. The exact numbers depend on which sections are included. Do not skip numbers.

## Condensed Template: Visual FB

```markdown
# <Name> -- <Русское название>

> **TL;DR:** <Краткое описание.>

## 1. Интерфейс

### Входы

| Имя | Тип | Описание |
|-----|-----|----------|
| <PinName> | <Тип> | <Описание> |

### Группа <GroupName>               ← only if XML has <Group> elements

| Имя | Тип | Описание |
|-----|-----|----------|

<Пояснение к группе, если необходимо.>

### Выходы

| Имя | Тип | Описание |
|-----|-----|----------|

### События (Warnings)

| Имя | Условие |
|-----|---------|

<Примечания к событиям.>

## 2. Визуальные свойства (окно настроек)

| Свойство | По умолчанию | Описание |
|----------|--------------|----------|

### <EnumDisplayName> (<EnumTypeName>)

- `Value` -- описание

## 3. Логика работы

<Что происходит при переходе в Runtime, при вводе значения, при получении внешнего сигнала. Описывать наблюдаемое поведение, а не внутренний алгоритм.>

## 4. Валидация при вводе

<Что видит пользователь при вводе некорректных данных. Какие значения отклоняются и почему.>
```

## Condensed Template: Headless FB

```markdown
# <Name> -- <Русское название>

> **TL;DR:** <Краткое описание.>

## 1. Интерфейс

### Входы

| Имя | Тип | Описание |
|-----|-----|----------|

### Выходы

| Имя | Тип | Описание |
|-----|-----|----------|

### Параметры (окно настроек)

| Свойство | По умолчанию | Описание |
|----------|--------------|----------|

## 2. Логика работы

<Что делает ФБ при старте, по команде, при получении данных. Фазы, последовательность действий -- с точки зрения пользователя.>

## 3. Обработка ошибок

### Критические ошибки (прерывают операцию)

| Ситуация | Поведение |
|----------|-----------|

### Предупреждения (не прерывают операцию)

| Ситуация | Поведение |
|----------|-----------|

## 4. Логирование

- **Путь:** ...
- **Ротация:** ...
```

## After Writing

1. **Update `Docs/readme.md`**: add a row to the 3-column table of contents — `| № | [<Name>](<file>.md) | <короткое описание> |` — using the next sequential `№`. Keep the description free of internal-mechanism terms (rule #1).
2. **Review**: re-read the doc against the source code. Verify every pin name, type, property name, default value, and event name matches the current code exactly.
3. **Do NOT commit** unless the user explicitly asks.

## Common Pitfalls

- **Leaking implementation details**: this is the most common mistake. The doc reader is an operator or integrator, not a developer. Do not mention: class names (`ValueArbiter`, `EventTrigger`), design patterns (debounce, polling, arbitration), internal constants (precision thresholds, timer intervals), field names, threading details, or how the control updates itself. Describe only what the user observes. If you catch yourself writing "internally" or "under the hood" -- delete that paragraph.
- **Including IDs**: pin IDs are implementation details. Do not add ID columns to tables.
- **Inventing behavior not in the code**: every statement in the doc must be traceable to a specific code path. If unsure, read the source.
- **Overly long TL;DR**: keep to 2-3 sentences. The TL;DR is a summary, not an introduction.
- **Borderline details -- guessing instead of asking**: if you are unsure whether a detail is user-actionable (e.g. "events are suppressed for 5 seconds after Runtime start"), ask the user instead of including it by default. Err on the side of omission.
- **Missing sections**: if the FB has events, document them. If it has visual properties, document them. Do not skip sections that apply.
