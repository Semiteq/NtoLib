# Несовпадение ID-констант пина с XML-маппингом → runtime NRE

## Описание проблемы

Visual FB полагается на ID-константы пинов в C#-коде, которые должны соответствовать записям в XML-маппингах (`<Map>` для UI и `<VisualMap>` для визуального слоя). Если ID-константа объявлена в коде, но в XML такого пина нет, обращение к ней через generic-helpers даёт `NullReferenceException` в runtime:

```
System.NullReferenceException: Object reference not set to an instance of an object.
   at NtoLib.Devices.Helpers.VisualFBBaseExtended.SetVisualAndUiPin(Int32 id, ...)
   at NtoLib.Devices.Helpers.VisualPins.SetPinValue(...)
```

**Симптом:** FB компилируется, юнит-тесты проходят, но как только FB запускается в SCADA-host'е и попадает в код, обращающийся к этому ID — падение.

## Причина

Generic-helpers вроде `SetVisualAndUiPin(int id, T value)` или `VisualPins.SetPinValue(int id, T value)` находят пин по ID через словарь, построенный из XML-маппинга. Если ID нет в словаре, helpers получают `null` и падают на первом разыменовании.

Компилятор не проверяет соответствие констант XML-маппингу — XML загружается как `<EmbeddedResource>` и парсится в runtime. Стандартные unit-тесты тоже не ловят расхождение, потому что в тестовом окружении нет реального FB-host'а с загруженным XML.

## Workaround / правило разработки

Порядок добавления нового пина:

1. **Сначала** добавить пин в XML — и в `<Map>`, и в `<VisualMap>` (если пин участвует в визуальном слое). ID должен быть уникален.
2. **Только после этого** добавить ID-константу в C#-коде.
3. **Только после этого** писать код, обращающийся к пину.

Если порядок нарушен — пока ID в коде есть, а в XML нет, любой `SetPinValue(id, ...)` упадёт в runtime.

## Чего нельзя делать

- Объявлять ID-константы «впрок», без соответствующего XML-пина.
- Менять ID существующего пина в одном из мест (XML или код), не синхронизируя с другим.
- Полагаться на компилятор или standalone unit-тесты для отлова расхождений — оба слоя не имеют доступа к runtime-словарю пинов.

## Диагностика

При получении `NullReferenceException` из `SetPinValue` или `SetVisualAndUiPin` **первое подозрение** — несовпадение ID, а не ошибка в бизнес-логике. Проверьте:

1. ID-константа в коде и пин с тем же ID в `<Map>` существуют.
2. Если используется визуальный слой — пин также есть в `<VisualMap>`.
3. Файл XML действительно собирается в DLL как `<EmbeddedResource>` (а не как `<Content>`), иначе он вообще не будет найден.

## Ссылки

- Helpers: `NtoLib/Devices/Helpers/VisualFBBaseExtended.cs`, `NtoLib/Devices/Helpers/VisualPins.cs`
- Reference XML: любой `*FB.xml` в `NtoLib/Devices/Valves/`, `NtoLib/Devices/Pumps/`
