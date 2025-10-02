#nullable enable

namespace NtoLib.Recipes.MbeTable.Errors;

public enum RecipeErrorCodes
{
    // Configuration (2xxx)
    ConfigFileNotFound = 2001,
    ConfigParseError = 2002,
    ConfigInvalidSchema = 2003,
    ConfigDuplicateId = 2004,
    ConfigMissingReference = 2005,

    // Properties (4xxx)
    PropertyConversionFailed = 4001,
    PropertyValidationFailed = 4002,
    PropertyCalculationFailed = 4003,
    PropertyNotAvailable = 4004,

    // Presentation/UI (5xxx)

    /// ComboBox cell initialization failed due to missing context or invalid state.
    ComboBoxInitFailed = 5001,

    /// Cell rendering operation failed during CellFormatting event.
    CellRenderingFailed = 5002,

    /// ColorScheme update operation failed.
    ColorSchemeUpdateFailed = 5003,

    /// Invalid row index provided for DataGridView operation in VirtualMode.
    InvalidRowIndex = 5004,

    /// Invalid column index provided for DataGridView operation.
    InvalidColumnIndex = 5005,

    /// Table initialization failed due to missing configuration or dependencies.
    TableInitializationFailed = 5006,

    /// Cell value retrieval failed in VirtualMode.
    CellValueRetrievalFailed = 5007,
    
    /// ComboBox datasource strategy execution failed.
    DataSourceStrategyFailed = 5008,
    
    // Business (6xxx)
    BusinessInvalidOperation = 6001,
    BusinessInvariantViolation = 6002,

    // Files/IO (8xxx)
    IoReadError = 8001,
    IoWriteError = 8002
}

