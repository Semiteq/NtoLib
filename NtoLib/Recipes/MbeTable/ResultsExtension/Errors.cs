namespace NtoLib.Recipes.MbeTable.ResultsExtension;

public static class Errors
{
    // Property errors
    public static BilingualError PropertyNotFound(string propertyName) => new(
        $"Property '{propertyName}' not found in step",
        $"Свойство '{propertyName}' не найдено в шаге");

    public static BilingualError PropertyTypeMismatch(string expectedType, string actualType) => new(
        $"Property type mismatch: expected {expectedType}, got {actualType}",
        $"Несоответствие типа свойства: ожидается {expectedType}, получен {actualType}");

    public static BilingualError PropertyValidationFailed(string reason) => new(
        $"Validation failed: {reason}",
        $"Ошибка валидации: {reason}");

    public static BilingualError PropertyConversionFailed(string input, string targetType) => new(
        $"Unable to parse '{input}' as {targetType}",
        $"Не удалось преобразовать '{input}' в {targetType}");

    public static BilingualError PropertyNonNumeric() => new(
        "Property holds a non-numeric string value",
        "Свойство содержит нечисловое строковое значение");

    public static BilingualError PropertyDefaultValueFailed(string value) => new(
        $"Failed to set default value for column '{value}'",
        $"Не удалось установить значение по умолчанию для столбца '{value}'");

    public static BilingualError PropertyParsingFailed(string defValue, string key) => new(
        $"Failed to parse default value '{defValue}' for column '{key}'",
        $"Не удалось разобрать значение по умолчанию '{defValue}' для столбца '{key}'");
    
    public static BilingualError PropertyCreationFailed(string key) => new(
        $"Failed to create property for column '{key}'",
        $"Не удалось создать свойство для столбца '{key}'");
    
    public static BilingualError ActionPropertyCreationFailed(short actionId) => new(
        $"Failed to create action property for action ID {actionId}",
        $"Не удалось создать свойство действия для действия с ID {actionId}");
    
    public static BilingualError TimeComponentOutOfRange(string component, int value, int maxValue) => new(
        $"Invalid {component} value: {value} (must be 0-{maxValue})",
        $"Недопустимое значение {component}: {value} (должно быть 0-{maxValue})");
    
    public static BilingualError StringLengthExceeded(int currentLength, int maxLength) => new(
        $"String length {currentLength} exceeds maximum allowed length of {maxLength}",
        $"Длина строки {currentLength} превышает максимально допустимую длину {maxLength}");
    
    public static BilingualError NumericValueOutOfRange(float value, float? min, float? max) => new(
        $"Value {value} is out of allowed range [{min ?? float.MinValue}, {max ?? float.MaxValue}]",
        $"Значение {value} находится вне допустимого диапазона [{min ?? float.MinValue}, {max ?? float.MaxValue}]");

    // Formula errors
    public static BilingualError FormulaNotFound(short actionId) => new(
        $"No compiled formula found for action ID {actionId}",
        $"Не найдена скомпилированная формула для действия с ID {actionId}");

    public static BilingualError FormulaVariableNotFound(string variableName) => new(
        $"Formula variable '{variableName}' not found in step properties",
        $"Переменная формулы '{variableName}' не найдена в свойствах шага");

    public static BilingualError FormulaVariableNonNumeric(string variableName) => new(
        $"Formula variable '{variableName}' has a non-numeric type",
        $"Переменная формулы '{variableName}' имеет нечисловой тип");

    public static BilingualError FormulaComputationFailed(string details) => new(
        $"Formula computation failed: {details}",
        $"Не удалось вычислить формулу: {details}");

    public static BilingualError FormulaDivisionByZero(string variable) => new(
        $"Division by zero while computing variable '{variable}'",
        $"Деление на ноль при вычислении переменной '{variable}'");

    public static BilingualError FormulaInvalidExpression() => new(
        "Failed to parse formula expression",
        "Не удалось разобрать выражение формулы");

    public static BilingualError FormulaEmptyExpression() => new(
        "Formula expression is empty",
        "Выражение формулы пустое");

    public static BilingualError FormulaEmptyRecalcOrder() => new(
        "Recalculation order is empty",
        "Порядок пересчета пуст");

    public static BilingualError FormulaRecalcOrderMissing(string variables) => new(
        $"Recalculation order is missing variables: {variables}",
        $"В порядке пересчета отсутствуют переменные: {variables}");

    public static BilingualError FormulaRecalcOrderExtra(string variables) => new(
        $"Recalculation order contains variables not present in formula: {variables}",
        $"Порядок пересчета содержит переменные, отсутствующие в формуле: {variables}");

    public static BilingualError FormulaNonLinear() => new(
        "Formula is non-linear (variable appears more than once)",
        "Формула нелинейная (переменная встречается более одного раза)");

    public static BilingualError FormulaTargetNotFound() => new(
        "No target variable found for recalculation",
        "Не найдена целевая переменная для пересчета");

    public static BilingualError FormulaVariableUnknown(string variableName) => new(
        $"Variable '{variableName}' is not known in formula",
        $"Переменная '{variableName}' не известна в формуле");

    // Step/Recipe errors
    public static BilingualError StepPropertyNotFound(string propertyKey, int rowIndex) => new(
        $"Property '{propertyKey}' not found in step at row {rowIndex}",
        $"Свойство '{propertyKey}' не найдено в шаге на строке {rowIndex}");

    public static BilingualError StepActionPropertyNull(int rowIndex) => new(
        $"Action property is null at row {rowIndex}",
        $"Свойство действия равно null на строке {rowIndex}");

    public static BilingualError StepPropertyUpdateFailed(int rowIndex, string columnName) => new(
        $"Failed to update property '{columnName}' at row {rowIndex}",
        $"Не удалось обновить свойство '{columnName}' на строке {rowIndex}");

    public static BilingualError IndexOutOfRange(int index, int count) => new(
        $"Index {index} is out of range (total: {count})",
        $"Индекс {index} вне диапазона (всего: {count})");

    public static BilingualError ActionNotFound(short actionId) => new(
        $"Action with ID {actionId} not found",
        $"Действие с ID {actionId} не найдено");

    public static BilingualError ActionNameNotFound(string actionName) => new(
        $"Action with name '{actionName}' not found",
        $"Действие с именем '{actionName}' не найдено");

    public static BilingualError ActionNameEmpty() => new(
        "Action name is empty",
        "Имя действия пустое");

    public static BilingualError NoActionsInConfig() => new(
        "No actions defined in configuration",
        "В конфигурации не определены действия");

    public static BilingualError ColumnNotFoundInAction(string actionName, short actionId, string columnKey) => new(
        $"Action '{actionName}' (ID: {actionId}) does not contain column '{columnKey}'",
        $"Действие '{actionName}' (ID: {actionId}) не содержит столбец '{columnKey}'");

    public static BilingualError ColumnGroupNameEmpty() => new(
        "Column GroupName is empty",
        "GroupName столбца пуст");

    public static BilingualError TargetsNotDefined() => new(
        "No targets defined for group",
        "Не определены цели для группы");

    // Validation errors
    public static BilingualError RecipeNull() => new(
        "Recipe cannot be null",
        "Рецепт не может быть null");

    public static BilingualError RecipeStepsNull() => new(
        "Recipe.Steps cannot be null",
        "Recipe.Steps не может быть null");

    public static BilingualError StepNull(int stepIndex) => new(
        $"Step at index {stepIndex} is null",
        $"Шаг с индексом {stepIndex} равен null");

    public static BilingualError StepMissingAction(int stepIndex) => new(
        $"Step at index {stepIndex} is missing Action property",
        $"Шаг с индексом {stepIndex} не содержит свойство Action");

    public static BilingualError StepActionNull(int stepIndex) => new(
        $"Step at index {stepIndex} has null Action property",
        $"Шаг с индексом {stepIndex} имеет null в свойстве Action");

    public static BilingualError StepNoActionProperty() => new(
        "Step does not have an action property",
        "Шаг не содержит свойство действия");

    public static BilingualError StepColumnNotFound() => new(
        "Step doesn't contain property",
        "Шаг не содержит свойство");
}