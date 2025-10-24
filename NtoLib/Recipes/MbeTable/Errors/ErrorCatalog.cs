using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Errors;

public sealed class ErrorCatalog : IErrorCatalog
{
    private readonly IReadOnlyDictionary<Codes, string> _map;
    private readonly IReadOnlyDictionary<int, string> _groupFallbacks;

    public ErrorCatalog()
    {
        _map = new Dictionary<Codes, string>
        {
            // === Конфигурация (2xxx) ===
            [Codes.ConfigFileNotFound] = "Файл конфигурации не найден",
            [Codes.ConfigParseError] = "Ошибка чтения файла конфигурации",
            [Codes.ConfigInvalidSchema] = "Некорректная структура файла конфигурации",
            [Codes.ConfigMissingReference] = "Обнаружена ссылка на несуществующий объект",
            [Codes.ConfigDuplicateValue] = "Обнаружены дублирующиеся значения в конфигурации",

            // === PLC / Modbus (3xxx) ===
            [Codes.PlcConnectionFailed] = "Не удалось подключиться к контроллеру",
            [Codes.PlcReadFailed] = "Ошибка чтения данных из контроллера",
            [Codes.PlcWriteFailed] = "Ошибка записи данных в контроллер",
            [Codes.PlcTimeout] = "Контроллер не отвечает (тайм-аут)",
            [Codes.PlcInvalidResponse] = "Получен некорректный ответ от контроллера",
            [Codes.PlcCapacityExceeded] = "Размер рецепта превышает память контроллера",
            [Codes.PlcVerificationFailed] = "Ошибка проверки: данные в ПЛК не совпадают с отправленными",
            [Codes.PlcZeroRowsRead] = "Рецепт в контроллере пуст",

            // === Операции с CSV файлами (4xxx) ===
            [Codes.CsvInvalidData] = "Некорректные данные или структура в CSV файле",
            [Codes.CsvHeaderMismatch] = "Заголовки в CSV файле не соответствуют текущей конфигурации",
            [Codes.CsvHashMismatch] = "CSV файл был изменен вне приложения",

            // === Операции с файлами / Ввод-вывод (5xxx) ===
            [Codes.IoReadError] = "Ошибка чтения файла",
            [Codes.IoWriteError] = "Ошибка записи файла",
            [Codes.IoFileNotFound] = "Выбранный файл не найден",

            // === Ядро бизнес-логики (6xxx) ===
            [Codes.CoreActionNotFound] = "Операция не найдена в конфигурации",
            [Codes.CoreColumnNotFound] = "Столбец не найден в конфигурации",
            [Codes.CorePropertyNotFound] = "Свойство не найдено в строке рецепта",
            [Codes.CoreTargetNotFound] = "Целевое устройство для операции недоступно",
            [Codes.CoreIndexOutOfRange] = "Номер строки или столбца вне допустимого диапазона",
            [Codes.CoreInvalidOperation] = "Недопустимая операция в данный момент",
            [Codes.CoreValidationFailed] = "Рецепт содержит ошибки валидации",
            [Codes.CoreForLoopError] = "Ошибка в структуре циклов (For/EndFor)",
            [Codes.CoreInvalidStepDuration] = "Обнаружена некорректная длительность шага",

            // === Свойства и типы данных (7xxx) ===
            [Codes.PropertyConversionFailed] = "Не удалось преобразовать введенное значение",
            [Codes.PropertyValidationFailed] = "Введенное значение не прошло проверку",
            [Codes.PropertyValueInvalid] = "Недопустимое значение для данного поля",
            
            // === Presentation / UI (8xxx) ===
            [Codes.UiOperationFailed] = "Не удалось выполнить операцию"
        };
        _groupFallbacks = new Dictionary<int, string>
        {
            { 1, "Произошла неизвестная ошибка" },
            { 2, "Ошибка конфигурации" },
            { 3, "Ошибка связи с ПЛК" },
            { 4, "Ошибка обработки CSV" },
            { 5, "Ошибка файловой системы" },
            { 6, "Ошибка выполнения операции" },
            { 7, "Ошибка данных" },
            { 8, "Ошибка интерфейса" }
        };
    }

    public bool TryGetMessage(Codes code, out string message)
    {
        return _map.TryGetValue(code, out message!);
    }

    public string GetMessageOrDefault(Codes code)
    {
        if (_map.TryGetValue(code, out var msg)) return msg;

        var codeInt = (int)code;
        var group = codeInt / 1000;
        if (_groupFallbacks.TryGetValue(group, out var groupMsg))
            return groupMsg;

        return "Operation failed";
    }
}