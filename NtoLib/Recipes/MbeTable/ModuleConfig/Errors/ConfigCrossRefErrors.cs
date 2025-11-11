using NtoLib.Recipes.MbeTable.ModuleConfig.Common;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public static class ConfigCrossRefErrors
{
    private const string Section = "ActionsDefs.yaml";

    public static ConfigError ReadOnlyDefaultConflict(string context, string columnKey) =>
        new ConfigError("Cannot set default_value for read_only column.", Section, context)
            .WithDetail("columnKey", columnKey);

    public static ConfigError DefaultValueNotInt16(string context, string value) =>
        new ConfigError($"default_value '{value}' is not a valid Int16.", Section, context)
            .WithDetail("defaultValue", value);

    public static ConfigError DefaultValueNotFloat(string context, string value) =>
        new ConfigError($"default_value '{value}' is not a valid Float.", Section, context)
            .WithDetail("defaultValue", value);

    public static ConfigError DefaultValueExceedsMaxLength(string context, int length, int maxLength, string value) =>
        new ConfigError($"default_value exceeds max_length. Value length: {length}, MaxLength: {maxLength}.", Section,
                context)
            .WithDetail("defaultValue", value)
            .WithDetail("maxLength", maxLength);

    public static ConfigError DefaultValueLessThanMin(string context, float value, float min) =>
        new ConfigError($"default_value {value} is less than min {min}.", Section, context)
            .WithDetail("defaultValue", value)
            .WithDetail("min", min);

    public static ConfigError DefaultValueExceedsMax(string context, float value, float max) =>
        new ConfigError($"default_value {value} exceeds max {max}.", Section, context)
            .WithDetail("defaultValue", value)
            .WithDetail("max", max);
}