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

    // Business (6xxx)
    BusinessInvalidOperation = 6001,
    BusinessInvariantViolation = 6002,

    // Files/IO (8xxx)
    IoReadError = 8001,
    IoWriteError = 8002
}