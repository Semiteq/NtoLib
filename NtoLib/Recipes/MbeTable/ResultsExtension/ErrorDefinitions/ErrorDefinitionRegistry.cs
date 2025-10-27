using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

public sealed class ErrorDefinitionRegistry
{
    private readonly IReadOnlyDictionary<Codes, ErrorDefinition> _definitions;

    public ErrorDefinitionRegistry()
    {
        _definitions = BuildDefinitions();
    }

    public ErrorDefinition GetDefinition(Codes code)
    {
        return _definitions.TryGetValue(code, out var definition) 
            ? definition 
            : new ErrorDefinition(
                code, 
                "Возникла неизвестная ошибка", 
                ErrorSeverity.Error, 
                BlockingScope.None);
    }

    public bool Blocks(Codes code, BlockingScope scope)
    {
        var definition = GetDefinition(code);
        return (definition.BlockingScope & scope) != 0;
    }

    public string GetMessage(Codes code) => GetDefinition(code).Message;

    private static IReadOnlyDictionary<Codes, ErrorDefinition> BuildDefinitions()
    {
        return new Dictionary<Codes, ErrorDefinition>
        {
            [Codes.UnknownError] = new(
                Codes.UnknownError,
                "Возникла неизвестная ошибка",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.ConfigFileNotFound] = new(
                Codes.ConfigFileNotFound,
                "Файл конфигурации не найден",
                ErrorSeverity.Critical,
                BlockingScope.AllOperations
            ),

            [Codes.ConfigParseError] = new(
                Codes.ConfigParseError,
                "Ошибка чтения файла конфигурации",
                ErrorSeverity.Critical,
                BlockingScope.AllOperations
            ),

            [Codes.ConfigInvalidSchema] = new(
                Codes.ConfigInvalidSchema,
                "Некорректная структура файла конфигурации",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.ConfigMissingReference] = new(
                Codes.ConfigMissingReference,
                "Обнаружена ссылка на несуществующий объект",
                ErrorSeverity.Error,
                BlockingScope.AllOperations
            ),

            [Codes.ConfigDuplicateValue] = new(
                Codes.ConfigDuplicateValue,
                "Обнаружены дублирующиеся значения в конфигурации",
                ErrorSeverity.Error,
                BlockingScope.AllOperations
            ),

            [Codes.PlcConnectionFailed] = new(
                Codes.PlcConnectionFailed,
                "Не удалось подключиться к контроллеру",
                ErrorSeverity.Critical,
                BlockingScope.AllOperations
            ),

            [Codes.PlcReadFailed] = new(
                Codes.PlcReadFailed,
                "Ошибка чтения данных из контроллера",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.PlcWriteFailed] = new(
                Codes.PlcWriteFailed,
                "Ошибка записи данных в контроллер",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.PlcTimeout] = new(
                Codes.PlcTimeout,
                "Контроллер не отвечает (тайм-аут)",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.PlcInvalidResponse] = new(
                Codes.PlcInvalidResponse,
                "Получен некорректный ответ от контроллера",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.PlcCapacityExceeded] = new(
                Codes.PlcCapacityExceeded,
                "Размер рецепта превышает память контроллера",
                ErrorSeverity.Error,
                BlockingScope.SaveAndSend
            ),

            [Codes.PlcVerificationFailed] = new(
                Codes.PlcVerificationFailed,
                "Ошибка проверки: данные в ПЛК не совпадают с отправленными",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.PlcZeroRowsRead] = new(
                Codes.PlcZeroRowsRead,
                "Рецепт в контроллере пуст",
                ErrorSeverity.Warning,
                BlockingScope.SaveAndSend
            ),
            
            [Codes.PlcFailedToPing] = new(
                Codes.PlcFailedToPing,
                "Контроллер не ответил на пинг",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CsvInvalidData] = new(
                Codes.CsvInvalidData,
                "Некорректные данные или структура в CSV файле",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CsvHeaderMismatch] = new(
                Codes.CsvHeaderMismatch,
                "Заголовки в CSV файле не соответствуют текущей конфигурации",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CsvHashMismatch] = new(
                Codes.CsvHashMismatch,
                "CSV файл был изменен вне приложения",
                ErrorSeverity.Warning,
                BlockingScope.None
            ),

            [Codes.IoReadError] = new(
                Codes.IoReadError,
                "Ошибка чтения файла",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.IoWriteError] = new(
                Codes.IoWriteError,
                "Ошибка записи файла",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.IoFileNotFound] = new(
                Codes.IoFileNotFound,
                "Выбранный файл не найден",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CoreActionNotFound] = new(
                Codes.CoreActionNotFound,
                "Операция не найдена в конфигурации",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CoreColumnNotFound] = new(
                Codes.CoreColumnNotFound,
                "Столбец не найден в конфигурации",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CorePropertyNotFound] = new(
                Codes.CorePropertyNotFound,
                "Свойство не найдено в строке рецепта",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CoreTargetNotFound] = new(
                Codes.CoreTargetNotFound,
                "Целевое устройство для операции недоступно",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CoreIndexOutOfRange] = new(
                Codes.CoreIndexOutOfRange,
                "Номер строки или столбца вне допустимого диапазона",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CoreInvalidOperation] = new(
                Codes.CoreInvalidOperation,
                "Недопустимая операция в данный момент",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CoreValidationFailed] = new(
                Codes.CoreValidationFailed,
                "Рецепт содержит ошибки валидации",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.CoreForLoopError] = new(
                Codes.CoreForLoopError,
                "Ошибка в структуре циклов (For/EndFor)",
                ErrorSeverity.Error,
                BlockingScope.SaveAndSend
            ),

            [Codes.CoreInvalidStepDuration] = new(
                Codes.CoreInvalidStepDuration,
                "Обнаружена некорректная длительность шага",
                ErrorSeverity.Error,
                BlockingScope.SaveAndSend
            ),
            
            [Codes.CoreExeedForLoopDepth] = new(
                Codes.CoreExeedForLoopDepth,
                "Превышена максимальная вложенность цикла For",
                ErrorSeverity.Error,
                BlockingScope.SaveAndSend
            ),
            
            [Codes.CoreEmptyRecipe] = new(
                Codes.CoreEmptyRecipe,
                "Рецепт не содержит шагов",
                ErrorSeverity.Warning,
                BlockingScope.SaveAndSend
            ),

            [Codes.PropertyConversionFailed] = new(
                Codes.PropertyConversionFailed,
                "Не удалось преобразовать введенное значение",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.PropertyValidationFailed] = new(
                Codes.PropertyValidationFailed,
                "Введенное значение не прошло проверку",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.PropertyValueInvalid] = new(
                Codes.PropertyValueInvalid,
                "Недопустимое значение для данного поля",
                ErrorSeverity.Error,
                BlockingScope.None
            ),

            [Codes.UiOperationFailed] = new(
                Codes.UiOperationFailed,
                "Не удалось выполнить операцию",
                ErrorSeverity.Warning,
                BlockingScope.None
            )
        };
    }
}