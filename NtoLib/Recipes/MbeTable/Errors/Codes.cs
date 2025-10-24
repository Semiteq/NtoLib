namespace NtoLib.Recipes.MbeTable.Errors;

public enum Codes
{
    UnknownError = 1000,

    // === Configs (2xxx) ===
    ConfigFileNotFound = 2001,
    ConfigParseError = 2002,
    ConfigInvalidSchema = 2003,
    ConfigMissingReference = 2004,
    ConfigDuplicateValue = 2005,

    // === PLC / Modbus (3xxx) ===
    PlcConnectionFailed = 3001,
    PlcReadFailed = 3002,
    PlcWriteFailed = 3003,
    PlcTimeout = 3004,
    PlcInvalidResponse = 3005,
    PlcCapacityExceeded = 3006,
    PlcVerificationFailed = 3007,
    PlcZeroRowsRead = 3008,

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

    // === Properties (7xxx) ===
    PropertyConversionFailed = 7001,
    PropertyValidationFailed = 7002,
    PropertyValueInvalid = 7003,

    // === Presentation / UI (8xxx) ===
    UiOperationFailed = 8001,
}