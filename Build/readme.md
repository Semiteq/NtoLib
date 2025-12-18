# Build and deployment (Windows)

Проект: **.NET Framework 4.8** (`net48`).

Сборка, упаковка и развёртывание выполняются PowerShell-скриптами из каталога `Build\`. Скрипты **не вычисляют пути автоматически**: все ключевые пути и конфигурация **задаются переменными окружения**.

Рекомемндуется работать через профили `.run` в Rider.

## Требования

На машине разработчика должны быть установлены:

- **.NET SDK** (`dotnet`)
- **Visual Studio Build Tools** с MSBuild для сборки `net48` (или полноценная Visual Studio)

## Переменные окружения

Скрипты используют следующие переменные:

### Обязательные (для сборки/тестов/упаковки)

- `REPO_ROOT`  
  Путь к корню репозитория, где расположен файл решения `NtoLib.sln`.  
  Пример: `C:\Users\admin\Projects\git\NtoLib`

- `BUILD_CONFIGURATION`  
  Конфигурация сборки: `Debug` или `Release`.

### Обязательные для развёртывания (Deploy)

- `NTOLIB_DEST_DIR`  
  Каталог назначения для копирования артефактов сборки. Это должна быть папка, из которой целевое приложение загружает библиотеку.

- `NTOLIB_CONFIG_DIR`  
  Каталог назначения для `DefaultConfig`.  
  Важно: при развёртывании каталог назначения **удаляется рекурсивно** и затем создаётся заново путём копирования `DefaultConfig`.

## Скрипты и назначение

### 1) Сборка (Build)

Скрипт: `Build\tools\Build.ps1`  
Назначение: сборка решения `NtoLib.sln` в заданной конфигурации.

Запуск:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\tools\Build.ps1
```

Ожидаемый результат:
- артефакты сборки появляются в `NtoLib\bin\<BUILD_CONFIGURATION>\`
- ключевой файл: `NtoLib\bin\<BUILD_CONFIGURATION>\NtoLib.dll`

### 2) Слияние зависимостей (Merge)

Скрипт: `Build\tools\Merge.ps1`  
Назначение: объединение `NtoLib.dll` и большинства managed-зависимостей в один файл с помощью **ILRepack**.

Скрипт:
- берёт все `*.dll` из `NtoLib\bin\<BUILD_CONFIGURATION>\`
- объединяет их в `NtoLib.dll`
- **не включает** хостовые зависимости MasterSCADA (например `FB.dll`, `MasterSCADA.*`) и **не включает** `System.Resources.Extensions.dll`
- использует `/lib:` для каталогов поиска зависимостей при анализе ссылок

Запуск:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\tools\Merge.ps1
```

Требование: установлен ILRepack в каталоге:
- `NtoLib\packages\ILRepack.2.0.44\tools\ILRepack.exe`

### 3) Тесты (Test)

Скрипт: `Build\tools\Test.ps1`  
Назначение: запуск тестов `Tests\Tests.csproj` в заданной конфигурации.

Запуск:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\tools\Test.ps1
```

Примечание: используется `dotnet test` без отключения restore/build (приоритет — стабильность для legacy-проекта).

### 4) Упаковка релиза (Package)

Скрипт: `Build\Package.ps1`  
Назначение: сборка, слияние зависимостей и формирование zip-архива в каталоге `Releases\` в корне репозитория.

Запуск:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\Package.ps1
```

Выходной артефакт:
- `Releases\NtoLib_v<version>.zip`
- `<version>` берётся из `NtoLib\Properties\AssemblyInfo.cs` из атрибута `AssemblyInformationalVersion`.

Содержимое архива включает:
- `NtoLib.dll` (после ILRepack)
- `System.Resources.Extensions.dll` (отдельным файлом, не объединяется)
- `DefaultConfig\` (если каталог существует)
- `NtoLib_reg.bat` (если файл существует)

### 5) Развёртывание (Deploy)

Скрипт: `Build\Deploy.ps1`  
Назначение: сборка, слияние зависимостей и копирование артефактов в папки установки/использования на локальной машине.

Запуск:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\Deploy.ps1
```

Что делает Deploy:
1. Выполняет сборку решения `NtoLib.sln` в конфигурации `BUILD_CONFIGURATION`.
2. Выполняет слияние зависимостей (`ILRepack`) для получения одного `NtoLib.dll`.
3. Копирует:
    - `NtoLib\bin\<BUILD_CONFIGURATION>\NtoLib.dll` → `%NTOLIB_DEST_DIR%\NtoLib.dll`
    - `NtoLib\bin\<BUILD_CONFIGURATION>\System.Resources.Extensions.dll` → `%NTOLIB_DEST_DIR%\System.Resources.Extensions.dll` (если существует)
    - `NtoLib\bin\<BUILD_CONFIGURATION>\NtoLib.pdb` → `%NTOLIB_DEST_DIR%\NtoLib.pdb` (если существует)
4. Если в репозитории есть каталог `DefaultConfig`, то:
    - удаляет `%NTOLIB_CONFIG_DIR%` рекурсивно (если существует)
    - копирует `DefaultConfig` в `%NTOLIB_CONFIG_DIR%`

## Порядок работы (рекомендуемый)

### Debug-развёртывание на своей машине (локальная отладка)

1. Установите переменные:
    - `REPO_ROOT` = путь к репозиторию
    - `BUILD_CONFIGURATION=Debug`
    - `NTOLIB_DEST_DIR` = папка, откуда целевое приложение загружает `NtoLib.dll`
    - `NTOLIB_CONFIG_DIR` = папка для конфигов (будет перезаписана)

2. Запустите:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\Deploy.ps1
```

3. Перезапустите целевое приложение/службу, чтобы новая библиотека была загружена.

### Release-сборка для передачи/установки

1. Установите:
    - `REPO_ROOT`
    - `BUILD_CONFIGURATION=Release`

2. Запустите упаковку:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\Package.ps1
```

3. Полученный файл `Releases\NtoLib_v<version>.zip` используйте для установки на целевой машине:
    - распакуйте `NtoLib.dll` и `System.Resources.Extensions.dll` в каталог, откуда приложение загружает библиотеку;
    - распакуйте `DefaultConfig` в требуемый каталог конфигурации (или используйте стандартный механизм конфигов вашей системы);
    - при необходимости выполните действия из `NtoLib_reg.bat` (если он включён и требуется вашей средой).

## Прямые команды dotnet (при необходимости)

Restore (packages.config):
```powershell
dotnet msbuild .\NtoLib.sln /t:Restore /p:RestorePackagesConfig=true
```

Build:
```powershell
dotnet build .\NtoLib.sln -c Release
```

Test:
```powershell
dotnet test .\Tests\Tests.csproj -c Release
```
