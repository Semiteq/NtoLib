

namespace NtoLib.Recipes.MbeTable.Journaling.Errors;

public enum ErrorCode
{
    // Configuration (2xxx)
    ConfigFileNotFound = 2001,
    ConfigParseError = 2002,
    ConfigInvalidSchema = 2003,
    ConfigDuplicateId = 2004,
    ConfigMissingReference = 2005,
    
    // PLC Communication (3xxx)
    PlcConnectionFailed = 3001,
    PlcReadFailed = 3002,
    PlcWriteFailed = 3003,
    PlcTimeout = 3004,
    PlcInvalidResponse = 3005,
    PlcInvalidRowCount = 3006,
    
    ModbusPingFailed = 3007,
    ModbusTcpLibraryError = 3008,
    ModbusGeneralError = 3009,
    
    // Properties (4xxx)
    PropertyConversionFailed = 4001,
    PropertyValidationFailed = 4002,
    PropertyCalculationFailed = 4003,
    PropertyNotAvailable = 4004,
    PropertyValueError = 4005,

    // Presentation/UI (5xxx)
    ComboBoxInitFailed = 5001, // ComboBox cell initialization failed due to missing context or invalid state.
    CellRenderingFailed = 5002, // Cell rendering operation failed during CellFormatting event.
    ColorSchemeUpdateFailed = 5003, // ColorScheme update operation failed.
    InvalidRowIndex = 5004, // Invalid row index provided for DataGridView operation in VirtualMode.
    InvalidColumnIndex = 5005, // Invalid column index provided for DataGridView operation.
    TableInitializationFailed = 5006,// Table initialization failed due to missing configuration or dependencies.
    CellValueRetrievalFailed = 5007, // Cell value retrieval failed in VirtualMode.
    DataSourceStrategyFailed = 5008, // ComboBox datasource strategy execution failed. 
    
    // Business (6xxx)
    BusinessInvalidOperation = 6001,
    BusinessInvariantViolation = 6002,

    // Files/IO (8xxx)
    IoReadError = 8001,
    IoWriteError = 8002,
    
    CoreNoActionFound = 9001,
    CoreForLoopFailure = 9002,
    CoreInvalidStepDuration = 9003, 
    CoreTargetNotFound = 9004,
    CoreIndexOutOfRange = 9005,
    CorePropertyNotFound = 9006,
    CoreNoSuchColumn = 9007,
    CoreInvalidColumnKey = 9008,
    CsvInvalidData = 9009,
}
