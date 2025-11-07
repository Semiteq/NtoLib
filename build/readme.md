# NtoLib — сборка с помощью NUKE

## TL;DR: команды

```bash
nuke BuildDebug --configuration Debug                     # Debug + копирование для локальной отладки
nuke BuildRelease --configuration Release                 # Release (тестовая/оптимизированная сборка)
nuke Package --configuration Release                      # Release + создание ZIP архива в Releases/
nuke Clean                                                # Очистка артефактов (для чистой сборки)
nuke Test                                                 # Все тесты (по умолчанию Release)
nuke Test --test-category Integration                     # Только интеграционные
nuke TestWithCoverage                                     # Тесты + HTML-отчёт о покрытии
```

---

## Назначение

NUKE-скрипт автоматизирует:

* очистку (`Clean`),
* восстановление пакетов (`Restore`),
* компиляцию (`Compile`),
* объединение зависимостей в один DLL (`ILRepack`),
* локальное копирование артефактов для отладки (`CopyToLocal` — только для Debug),
* запуск тестов (`Test`),
* создание релизного архива (`Package` — только для Release).

Скрипт ориентирован на .NET Framework 4.8 (C#10) и использует классическую папку `packages/` (packages.config-style).
ILRepack интегрирован как внешний исполняемый файл в `packages\ILRepack.<ver>\tools\ILRepack.exe`.
`System.Resources.Extensions.dll` всегда копируется отдельно (не встраивается), это необходимо для корректной загрузки
картинок для кнопок. При попытке встроить в общий DLL - рантайм не может найти зависимость, поэтому оставлено отдельно.

---

## Требования (обязательное)

* .NET SDK 9.x (должен быть установлен `dotnet`).
* MSBuild (Visual Studio 2022 или эквивалентный MSBuild в PATH).
* NUKE Global Tool (см. установка ниже).
* Наличие каталога `packages/` (в проекте используются пакеты в формате packages.config).
* Права записи в `Releases/` и (для Debug-деплоя) в `DestinationDirectory` (по умолчанию
  `C:\Program Files (x86)\MPSSoft\MasterSCADA`).
* ILRepack в `packages\ILRepack.<ver>\tools\ILRepack.exe` (скрипт не обновляет версию автоматически).

---

## Установка NUKE (первый запуск)

Установить глобальный инструмент [NUKE](https://nuke.build/).

---

## Как запускать (главные команды)

> Обратите внимание: **всегда** указывайте `--configuration` при запуске таргетов, чтобы значения `Configuration`
> использовались корректно в зависимости/условиях таргетов.

```bash
# Debug: сборка + копирование в локальный каталог (для отладки)
nuke BuildDebug --configuration Debug

# Release: сборка в Release (без архива)
nuke BuildRelease --configuration Release

# Полный релиз: сборка Release + создание zip-архива в Releases/
nuke Package --configuration Release

# Все тесты
nuke Test 

# Только Category=Integration
nuke Test --test-category Integration
 
# Только Component=ConfigLoader
nuke Test --test-component ConfigLoader

# Комбинация фильтров
nuke Test --test-category Integration --test-component FormulaPrecompiler 

# Тесты с coverage (генерирует отчёт в temp, выводит summary в консоль, генерирует HTML)
nuke TestWithCoverage 

# Coverage для Debug-сборки
nuke TestWithCoverage --configuration Debug 

# Очистка (удаление артефактов, bin/obj, создание пустого Releases/)
nuke Clean
```

---

## Поведение таргетов — детально

### Clean

* Удаляет `bin/` и `obj/` по всему решению (исключая папки `packages` и `build`), очищает `Releases/`.
* Обрабатывает ошибки удаления (файл/папка заняты) — не падает, логирует предупреждение.

### Restore

* Запускает `msbuild /t:Restore` для решения (восстановление пакетов NuGet / классических packages).

### Compile

* Запускает `msbuild /t:Build` с конфигурацией, соответствующей `--configuration`.
* Для Release включаются оптимизации, для Debug — подробный PDB (`DebugType=full`).
* MSBuildverbosity отображается в логах в соответствии с параметром `Verbosity` в NUKE (по умолчанию `Minimal`/
  `Normal`).

### ILRepack

* Собирает список входных сборок: основной `NtoLib.dll` + перечень NuGet-зависимостей (список перечислен в скрипте).
* Если в NtoLib были изменения (timestamp) или изменились входные зависимости — выполняет слияние.
* В конфигурации исключены системные `System.*` из GAC и крупные внешние библиотеки MasterSCADA (они не мержатся).

### Test

* Запускает `dotnet test` для проекта `NtoLib.Test`.
* Использует `--no-build --no-restore` (предполагается, что Compile уже выполнен).
* Поддерживает фильтрацию по Trait-атрибутам (`--test-category`, `--test-component`).

### TestWithCoverage

* Запускает тесты с включённым сбором Code Coverage (coverlet).
* Генерирует HTML-отчёт в `TemporaryDirectory/coverage/html/` (автоочищается).
* Выводит краткую сводку (line coverage %) в консоль из `Summary.txt`.

### CopyToLocal (локальный деплой)

* Выполняется только в Debug (условие `OnlyWhenDynamic(() => Configuration == Configuration.Debug)`).
* Сравнивает timestamps/наличие файлов перед копированием (инкрементность).
* Копирует только `FilesToDeploy` (например, `NtoLib.dll`, `NtoLib.pdb`, `System.Resources.Extensions.dll`) и папку
  `NtoLibTableConfig/`.
* При отсутствии или устаревании — копирует; в противном случае пропускает.

### Package

* Работает только для Release.
* Берёт версию из `AssemblyInformationalVersion` в `Properties/AssemblyInfo.cs`.
* Создаёт временную папку, копирует необходимые файлы + `NtoLib_reg.bat` + `NtoLibTableConfig/`, запаковывает в
  `Releases/NtoLib_v<версия>.zip`.
* `System.Resources.Extensions.dll` добавляется отдельно (не встраивается в merged DLL).

---

## Файлы/зависимости: что включается в объединённый DLL (ILRepack) и что не включается

**Включаются (из NuGet / backport-пакетов):**

См. `string[] AssembliesToMerge`. Список может меняться.

**НЕ включаются:**

* System.Resources.Extensions.dll, см выше почему.
* Внутренние/платформенные библиотеки MasterSCADA (MasterSCADA.Common, MasterSCADALib, InSAT.Library, Insat.Opc, FB) —
  предполагается, что они присутствуют на целевых машинах и не подлежат слиянию.

---

## Инкрементальность и оптимизация

* MSBuild обеспечивает инкрементальную компиляцию по умолчанию — не пытайтесь дублировать этот механизм на уровне
  скрипта.
* Скрипт реализует инкрементальные проверки **для ILRepack** (timestamp сравнение) и **для CopyToLocal** (
  timestamp/наличие). Это снижает лишние пересборки/копирования.
* Для максимальной корректности рекомендуем:

    * не менять `Configuration` внутри выполнения таргетов — передавайте `--configuration`.
    * избегать ручного удаления `packages/` между запусками, если версии пакетов не менялись.

---

## Тесты

* Проект `NtoLib.Test` содержит интеграционные тесты.
* В CI/CD и перед релизом тесты запускаются через NUKE (`nuke Test`).
* Проект `NtoLib.Test` содержит интеграционные тесты для модуля конфигурации (`ModuleConfig`).
* Тесты используют реальные YAML-файлы из `TestData/Valid` и `TestData/Invalid`.
* Для локальной разработки тесты запускаются через IDE (быстрый прогон/отладка).
* В CI/CD и перед релизом тесты запускаются через NUKE (`nuke Test` / `nuke TestWithCoverage`).

### Фильтрация тестов

Используйте параметры `--test-category` и `--test-component` для запуска подмножества тестов:

```bash
nuke Test                                                                 # all tests
nuke Test --test-category Integration                                     # Category=Integration
nuke Test --test-component ConfigLoader                                   # Component=ConfigLoader
nuke Test --test-category Integration --test-component FormulaPrecompiler # Only Integrations for FormulaPrecompiler
```

### Code Coverage

* Target TestWithCoverage собирает метрики покрытия кода (coverlet).
* HTML-отчёт генерируется во временной папке (не сохраняется в репозиторий).
* Краткая сводка выводится в консоль:

```
[INF] Coverage Summary: 
[INF]   Coverage: 85.3% (1234 of 1447 lines)
```

* Coverage запускается автоматически при nuke Package (релиз-сборка).

---

## Логирование и диагностика

* NUKE использует Serilog (скрипт настроен на использование уровней `Information`, `Debug`, `Warning`, `Error`).
* Для подробного вывода работы MSBuild используйте флаг verbosity NUKE/MSBuild:

```bash
nuke BuildDebug --configuration Debug --verbosity verbose
```

* При нормальной работе (`Minimal`/`Normal`) вывод показывает ключевые этапы, при `Verbose` — детальный вывод
  ILRepack/MSBuild.