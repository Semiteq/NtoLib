# Build and deployment (Windows)

Проект: **.NET Framework 4.8** (`net48`), .NET SDK **8.x** (см. `global.json`).

В каталоге `Build\` остался только один скрипт — **`Deploy.ps1`** для локального дебаг-развёртывания библиотеки в установленный MasterSCADA. Сборка релизных артефактов выполняется в GitHub Actions (`.github/workflows/release.yml`) по пушу тега `v*`.

Рекомендуется работать через профили `.run` в Rider.

## Требования

- **.NET SDK** (`dotnet`) — версия из `global.json`
- **Visual Studio Build Tools** с MSBuild для `net48` (или полноценная Visual Studio)

## Переменные окружения для `Build\Deploy.ps1`

- `REPO_ROOT` — путь к корню репозитория, где лежит `NtoLib.sln`. Пример: `C:\Users\admin\projects\NtoLib`.
- `BUILD_CONFIGURATION` — `Debug` или `Release`.
- `NTOLIB_DEST_DIR` — каталог, из которого MasterSCADA подгружает `NtoLib.dll`. Пример: `C:\Program Files (x86)\MPSSoft\MasterSCADA`.
- `NTOLIB_CONFIG_DIR` — каталог для копирования содержимого `DefaultConfig\`. Пример: `C:\DISTR\Config`.

В Rider переменные инжектятся через `.run\Deploy Debug.run.xml`.

## Что делает `Deploy.ps1`

1. `dotnet build NtoLib.sln -c $cfg` — сборка решения (un-merged, нужно для тестов в CI; локально просто промежуточный шаг).
2. `dotnet build NtoLib\NtoLib.csproj -c $cfg -p:RunILRepack=true` — слияние NuGet-зависимостей в `NtoLib.dll` через MSBuild-таргет `NtoLib/ILRepack.targets`. Vendor SDK DLL (`FB.dll`, `MasterSCADA.*`) и `System.Resources.Extensions.dll` **не сливаются**.
3. Копирует артефакты в `NTOLIB_DEST_DIR`:
    - `NtoLib.dll`
    - `NtoLib.pdb` (если есть)
    - `System.Resources.Extensions.dll` (если есть)
4. Копирует `DefaultConfig\*` в `NTOLIB_CONFIG_DIR`.

Запуск напрямую:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build\Deploy.ps1
```

После успешного `Deploy.ps1` перезапустите MasterSCADA, чтобы новая библиотека была подгружена.

## Релиз (выполняется в CI)

Локального релизного скрипта **больше нет**. Релизный конвейер живёт в `.github/workflows/release.yml`.

Чтобы выпустить релиз:

1. Создайте annotated git tag вида `vX.Y.Z` (или с pre-release суффиксом, например `v1.12.0-beta1`):
    ```powershell
    git tag -a v1.12.0 -m "release notes here"
    git push origin v1.12.0
    ```
2. CI на windows-2025 раннере: build → test → ILRepack → собирает `NtoLib_v<ver>.zip` + `Installer.exe` (с версией из тега) → создаёт GitHub Release с обоими файлами.
3. Версия из тега прокидывается через `-p:Version=$VERSION` (csproj — `GenerateAssemblyInfo=true`).

Локальные сборки используют fallback-версию из `NtoLib/NtoLib.csproj` (`<Version>`), CI всегда переопределяет тегом.

## Прямые команды `dotnet` (при необходимости)

```powershell
dotnet restore .\NtoLib.sln
dotnet build .\NtoLib.sln -c Release
dotnet test .\NtoLib.sln -c Release
dotnet format .\NtoLib.sln --verify-no-changes
```
