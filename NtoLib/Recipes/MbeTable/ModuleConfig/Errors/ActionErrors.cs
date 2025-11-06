using NtoLib.Recipes.MbeTable.ModuleConfig.Common;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public static class ActionErrors
{
    private const string Section = "ActionsDefs.yaml";

    public static ConfigError UnknownDeployDuration(string value, string context) =>
        new ConfigError($"Unknown deploy_duration '{value}'. Allowed: Immediate, LongLasting.", Section, context)
            .WithDetail("deployDuration", value);

    public static ConfigError EnumGroupMissing(string columnKey, string propertyTypeId, string context) =>
        new ConfigError("Column with property_type_id='Enum' must have a 'group_name'.", Section, context)
            .WithDetail("columnKey", columnKey)
            .WithDetail("propertyTypeId", propertyTypeId);

    public static ConfigError LongLastingStepDurationMissing(string context) =>
        new("Action with deploy_duration='LongLasting' must include column 'step_duration'.", Section, context);
}