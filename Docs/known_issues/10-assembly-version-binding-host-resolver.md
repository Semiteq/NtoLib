# Хост перекрывает несовпадение версий strong-name (внешние NuGet-зависимости)

## Описание проблемы

`NtoLib.dll` может держать **внешнюю** ссылку на strong-named NuGet-сборку, версия
которой не совпадает с тем, что физически лежит рядом с DLL в папке MasterSCADA. По
правилам загрузчика .NET Framework это должно давать `FileLoadException` в рантайме, но
на практике не даёт — хост MasterSCADA перекрывает бинд собственным резолвером. Это и
объясняет, почему «дрейф версий» не всплывает, и одновременно является скрытой ловушкой.

Разобрано на примере `System.Text.Json`, но относится к любой strong-named зависимости,
оставленной вне merge (см. exclude-лист в `NtoLib/ILRepack.targets`).

## Конкретика на System.Text.Json

- Смёрженная `NtoLib.dll` ссылается на `System.Text.Json, Version=10.0.0.8,
  PublicKeyToken=cc7b13ffcd2ddd51` и `System.Text.Encodings.Web, 10.0.0.8` — **внешне**
  (обе в exclude-листе ILRepack, в merge и в поставку не попадают).
- Хост несёт рядом с DLL свою `System.Text.Json 4.0.1.2` (эпоха .NET Core 3.x) и
  `System.Text.Encodings.Web 4.0.5.0`. Ни 10.x в GAC, ни binding redirect в конфигах
  папки — нет.
- Единственный потребитель в коде — `OpcTreeManager` (`Config/TreeSnapshotWriter.cs`,
  `Config/TreeSnapshotLoader.cs`).

## Почему это должно падать — и не падает

Для strong-named ссылки (ненулевой `PublicKeyToken`) загрузчик .NET Framework требует
**точного совпадения версии** манифеста с версией ссылки (после применения возможных
binding redirect). Есть только `4.0.1.2` < запрошенной `10.0.0.8`, редиректа нет →
`FileLoadException`, HRESULT `0x80131040` («manifest definition does not match the
assembly reference»). Это подтверждается изолированным запуском (см. «Воспроизведение»,
случай A).

Но MasterSCADA — плагин-хост, грузящий множество сторонних .NET-компонентов, — ставит
**process-wide `AppDomain.AssemblyResolve`, резолвящий по простому имени** (файл в папке
установки). Ключевой факт: **сборка, возвращённая из `AssemblyResolve`, принимается
загрузчиком в обход проверки версии strong-name.** Поэтому ссылка `10.0.0.8`
связывается с файлом `4.0.1.2`. NtoLib использует только базовый API STJ
(`JsonSerializer.Serialize/Deserialize<T>`, `WriteIndented`, `PropertyNameCaseInsensitive`),
который есть с первой версии, — всё отрабатывает.

Своего `AssemblyResolve` у NtoLib нет (в коде отсутствует). Резолвер — хостовый.

Подтверждено рантайм-логом `OpcTreeManager` на реальном хосте: снапшот десериализовался,
план построился, `Execution complete … fail=0`, `Deferred execution completed
successfully`. Если бы бинд падал, `TreeSnapshotLoader.Load` вернул бы failed `Result`
(исключение ловит `Result.Try`), и `ScanAndValidate` прервался бы с `[ERR]` — плана и
deferred-исполнения не было бы.

## Воспроизведение

net48-экзешник, скомпилированный против `System.Text.Json 10.0.8` (как NtoLib), в папке,
где рядом только хостовые `4.x` (STJ `4.0.1.2` + её транзитивные зависимости):

| Случай | Настройка | Итог |
|--------|-----------|------|
| A | сырой бинд, без резолвера | `FileLoadException 0x80131040` — версия не сошлась |
| B | `AssemblyResolve` по простому имени, поставлен до JIT метода-потребителя | резолвер отдаёт `4.0.1.2` → **`OK`, `JsonSerializer resolved to v4.0.1.2`** |

Тонкость случая B: резолвер обязан стоять **до** JIT-компиляции метода, который трогает
STJ. JIT резолвит зависимости при компиляции всего тела метода, поэтому нельзя ставить
хендлер и вызывать `JsonSerializer` в одном методе — бинд произойдёт раньше установки
хендлера. Хост ставит резолвер на старте AppDomain, задолго до джита методов NtoLib.

## Почему это ловушка, а не «всё хорошо»

Работоспособность держится на двух неявных контрактах, **невидимых для xUnit** (в dev
NuGet кладёт настоящую `10.x` рядом, там ссылка резолвится штатно, тест зелёный):

1. Хост продолжает ставить резолвер по имени и поставлять какую-то `System.Text.Json`.
   Уберёт резолвер, переедет папка, сменится модель загрузки — бинд снова упадёт, уже в
   продакшене.
2. NtoLib не начнёт использовать API новее хостовой версии. Любой вызов из STJ 5+/8+
   (позиционные `record` при десериализации, `JsonNode`, source-gen,
   `JsonSerializerOptions.Default`) на `4.0.1.2` даст `MissingMethodException` /
   `MissingFieldException` в рантайме — и снова тесты на 10.x этого не поймают.

Симптом при срабатывании ловушки — не MOTW (`0x80131515`, см.
[04-deployment-errors.md](04-deployment-errors.md)), а `FileLoadException 0x80131040`
(версия) либо `MissingMethodException` (API), уже после успешной регистрации, в момент
первого обращения к соответствующему коду.

## Решение / профилактика

- **Предпочтительно: интернализировать** такие зависимости в `NtoLib.dll` — убрать из
  exclude-листа `NtoLib/ILRepack.targets`. Приватная копия версии, против которой собран
  код, живёт внутри сборки; хостовая версия не участвует, оба контракта снимаются. Так
  безопасно именно потому, что типы зависимости не пересекают COM-границу к хосту
  (NtoLib использует их для себя). Отслеживается в issue #127.
- **Исключение — «не-mergeable» сборки** (`System.Resources.Extensions` и т.п.), которые
  ломаются при интернализации и обязаны оставаться внешними; их доставлять файлом рядом с
  DLL (см. [02-dll-merge-constraints.md](02-dll-merge-constraints.md)).
- Если зависимость намеренно оставлена внешней и завязана на хостовую версию — ограничить
  используемый API уровнем этой версии и зафиксировать это (тест/линт), иначе дрейф API
  всплывёт только в бою.

## Диагностика

Ground-truth-проверка, что и откуда связалось в хосте — Fusion binding log:

```
reg add "HKLM\SOFTWARE\Microsoft\Fusion" /v ForceLog /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Fusion" /v LogFailures /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Fusion" /v LogPath /t REG_SZ /d "C:\FusionLog\" /f
```

Перезапустить MasterSCADA, выполнить операцию, затронувшую нужный код, посмотреть логи в
`C:\FusionLog\`. По завершении диагностики удалить эти ключи (`ForceLog` замедляет
загрузку сборок во всей системе).

Статическая проверка внешних ссылок смёрженной DLL:

```powershell
([Reflection.Assembly]::ReflectionOnlyLoadFrom('NtoLib.dll')).GetReferencedAssemblies() |
  ForEach-Object { '{0}  {1}' -f $_.Name, $_.Version }
```

Версия сборки рядом с DLL:

```powershell
[Reflection.AssemblyName]::GetAssemblyName('<путь>\System.Text.Json.dll').Version
```

> Примечание: `dotpeek`/статические анализаторы резолвят зависимости по простому имени и
> **игнорируют версию strong-name** — «битых зависимостей не найдено» у них не доказывает,
> что рантайм-бинд по версии сойдётся.
