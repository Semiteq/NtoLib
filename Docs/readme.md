# NtoLib: Документация

Библиотека пользовательских функциональных блоков для MasterSCADA 3.12.

---

## Содержание

| № | Раздел | Описание |
|---|--------|----------|
| 1 | [ConfigLoader](config-loader.md) | Загрузка/сохранение именованных настроек оборудования из YAML |
| 2 | [TrendPensManager](trend-pens-manager.md) | Автоматизация настройки перьев трендов |
| 3 | [LinkSwitcher](link-switcher.md) | Перелинковка связей между структурно идентичными объектами |
| 4 | [MbeTable](MbeTable/readme.md) | Таблица рецептов MBE: конфигурация, UI, Modbus TCP, CSV |
| 5 | [NumericBox](numeric-box.md) | Поле ввода числового значения с арбитрацией и валидацией |
| 6 | [Installer](installer.md) | Графический установщик NtoLib: копирование файлов, конфигурации и COM-регистрация |

---

## Известные проблемы платформы

| Проблема | Описание |
|----------|----------|
| [BackColorTransparent](KnownIssues/01-back-color-transparent.md) | Зависание UI при `BackColor = Color.Transparent` |
| [DllMergeConstraints](KnownIssues/02-dll-merge-constraints.md) | Ограничения сборки DLL (Costura, ILRepack) |
| [ProjectCachingAndSerialization](KnownIssues/03-project-caching-and-serialization.md) | Кэширование и сериализация компонентов в проекте |
| [DeploymentErrors](KnownIssues/04-deployment-errors.md) | Ошибки при развёртывании и регистрации NtoLib |
