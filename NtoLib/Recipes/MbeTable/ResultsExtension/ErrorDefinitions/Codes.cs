namespace NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

public enum Codes
{
    UnknownError = 1000,

    // === Configs (2xxx) ===
    ConfigDirectoryNotFound = 2001,
    ConfigFileNotFound = 2002,
    ConfigParseError = 2003,
    ConfigInvalidSchema = 2004,
    ConfigMissingReference = 2005,
    ConfigDuplicateValue = 2006,
    FormulaValidationFailed = 2100,
    FormulaNonLinear = 2101,
    FormulaInvalidExpression = 2102,
    FormulaIncompleteRecalcOrder = 2103,
    FormulaVariableMismatch = 2104,

    // === PLC / Modbus (3xxx) ===
    PlcConnectionFailed = 3001,
    PlcReadFailed = 3002,
    PlcWriteFailed = 3003,
    PlcTimeout = 3004,
    PlcInvalidResponse = 3005,
    PlcCapacityExceeded = 3006,
    PlcVerificationFailed = 3007,
    PlcZeroRowsRead = 3008,
    PlcFailedToPing = 3009,
    PlcRecipeSerializationPropertyNotFound = 3010,

    // === CSV (4xxx) ===
    CsvInvalidData = 4001,
    CsvHeaderMismatch = 4002,
    CsvHashMismatch = 4003,

    // === I/O (5xxx) ===
    IoReadError = 5001,
    IoWriteError = 5002,
    IoFileNotFound = 5003,

    // === Core (6xxx) ===
    CoreActionNotFound = 6001,
    CoreColumnNotFound = 6002,
    CorePropertyNotFound = 6003,
    CoreTargetNotFound = 6004,
    CoreIndexOutOfRange = 6005,
    CoreInvalidOperation = 6006,
    CoreValidationFailed = 6007,
    CoreForLoopError = 6008,
    CoreInvalidStepDuration = 6009,
    CoreExeedForLoopDepth = 6010,
    CoreEmptyRecipe = 6011,
    CorePropertyTypeMismatch = 6012,
    
    FormulaRecalcOrderMissingVars = 6095,
    FormulaRecalcOrderMissingVariables = 6096,
    FormulaEmptyRecalcOrder = 6098,
    FormulaEmptyExpression = 6099,
    FormulaComputationFailed = 6100,
    FormulaConstraintViolation = 6101,
    FormulaDivisionByZero = 6102,
    FormulaNotFound = 6103,
    FormulaInvalidVariableType = 6104,
    
    // === Properties (7xxx) ===
    PropertyConversionFailed = 7001,
    PropertyValidationFailed = 7002,
    PropertyValueInvalid = 7003,

    // === Presentation / UI (8xxx) ===
    UiOperationFailed = 8001,
}