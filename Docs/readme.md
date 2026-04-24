# NtoLib: Документация

Библиотека пользовательских функциональных блоков для MasterSCADA 3.12.

---

## Содержание

| № | Раздел | Описание |
|---|--------|----------|
| 1 | [ConfigLoader](config-loader.md) | Загрузка/сохранение именованных настроек оборудования из YAML |
| 2 | [TrendPensManager](trend-pens-manager.md) | Автоматизация настройки перьев трендов |
| 3 | [LinkSwitcher](link-switcher.md) | Перелинковка связей между структурно идентичными объектами |
| 4 | [MbeTable](mbe_table/readme.md) | Таблица рецептов MBE: конфигурация, UI, Modbus TCP, CSV |
| 5 | [NumericBox](numeric-box.md) | Поле ввода числового значения с арбитрацией и валидацией |
| 6 | [Installer](installer.md) | Графический установщик NtoLib: копирование файлов, конфигурации и COM-регистрация |
| 7 | [OpcTreeManager](opc-tree-manager.md) | Перестройка дерева OPC UA FB под выбранный целевой проект с произвольной глубиной вложенности (shrink/expand с восстановлением связей на любом уровне) |

---

## Архитектура (для разработчиков и LLM-агентов)

Документы ниже описывают правила работы с кодом, а не продуктовые возможности.
Лежат в `architecture/` — отдельно от пользовательской документации.

| Документ | Описание |
|----------|----------|
| [architecture/masterscada-fb-primer.md](architecture/masterscada-fb-primer.md) | Краткий справочник по платформе MasterSCADA 3.12: виды FB, иерархия базовых классов, пин-система, жизненный цикл, XML-конфиг, COM-регистрация, threading, список платформенных подводных камней |
| [architecture/architecture.md](architecture/architecture.md) | NtoLib-специфичные паттерны поверх платформы: 4-слойный Visual FB, thin-orchestrator Headless FB, шаблон отложенного исполнения, file-based logging, структура тестов OpcTreeManager |

---

## Известные проблемы платформы

| Проблема | Описание |
|----------|----------|
| [BackColorTransparent](known_issues/01-back-color-transparent.md) | Зависание UI при `BackColor = Color.Transparent` |
| [DllMergeConstraints](known_issues/02-dll-merge-constraints.md) | Ограничения сборки DLL (Costura, ILRepack) |
| [ProjectCachingAndSerialization](known_issues/03-project-caching-and-serialization.md) | Кэширование и сериализация компонентов в проекте |
| [DeploymentErrors](known_issues/04-deployment-errors.md) | Ошибки при развёртывании и регистрации NtoLib |
| [OpcCommandPinConnectOverload](known_issues/05-opc-command-pin-connect-overload.md) | `PlanExecutor` обязан использовать no-arg `Connect` для Command-пинов |
| [RuntimeTreeModificationForbidden](known_issues/06-runtime-tree-modification-forbidden.md) | Запрет модификации дерева в runtime; shutdown-последовательность и отложенное выполнение |
| [FbInstanceReplacement](known_issues/07-fb-instance-replacement.md) | MasterSCADA пересоздаёт FB между runtime-циклами; результат отложенного выполнения нужно писать в файл |
| [BeginInvokeRepostingFails](known_issues/08-begininvoke-reposting-fails.md) | Self-reposting `BeginInvoke` выгорает за микросекунды; нужен `WinForms.Timer` |
| [MismatchedPinIds](known_issues/09-mismatched-pin-ids.md) | Несовпадение ID-констант с XML-маппингом → runtime `NullReferenceException` |
