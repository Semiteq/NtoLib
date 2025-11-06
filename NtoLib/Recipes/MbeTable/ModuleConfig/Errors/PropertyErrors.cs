using NtoLib.Recipes.MbeTable.ModuleConfig.Common;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public static class PropertyErrors
{
    private const string Section = "PropertyDefs.yaml";

    public static ConfigError UnknownSystemType(string propertyTypeId, string systemType, string context) =>
        new ConfigError($"Unknown SystemType '{systemType}'.", Section, context)
            .WithDetail("PropertyTypeId", propertyTypeId)
            .WithDetail("SystemType", systemType);

    public static ConfigError UnsupportedSystemType(string propertyTypeId, string systemType, string supported,
        string context) =>
        new ConfigError($"Unsupported SystemType '{systemType}'. Supported: {supported}.", Section, context)
            .WithDetail("PropertyTypeId", propertyTypeId)
            .WithDetail("SystemType", systemType)
            .WithDetail("SupportedTypes", supported);

    public static ConfigError UnsupportedFormatKind(string propertyTypeId, string formatKind, string supported,
        string context) =>
        new ConfigError($"Unsupported format_kind '{formatKind}'. Supported: {supported}.", Section, context)
            .WithDetail("PropertyTypeId", propertyTypeId)
            .WithDetail("FormatKind", formatKind)
            .WithDetail("SupportedFormats", supported);

    public static ConfigError TimeHmsRequiresTime(string propertyTypeId, string context) =>
        new ConfigError("format_kind='TimeHms' can only be used with PropertyTypeId='Time'.", Section, context)
            .WithDetail("PropertyTypeId", propertyTypeId)
            .WithDetail("FormatKind", "TimeHms");

    public static ConfigError ScientificRequiresNumeric(string propertyTypeId, string systemType, string context) =>
        new ConfigError("format_kind='Scientific' can only be used with numeric types.", Section, context)
            .WithDetail("PropertyTypeId", propertyTypeId)
            .WithDetail("SystemType", systemType)
            .WithDetail("FormatKind", "Scientific");
}