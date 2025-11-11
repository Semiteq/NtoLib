using NtoLib.Recipes.MbeTable.ModuleConfig.Common;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public static class ConfigColumnErrors
{
    private const string Section = "ColumnDefs.yaml";

    public static ConfigError MissingMandatory(string missingList) =>
        new ConfigError($"Missing mandatory columns: {missingList}.", Section, "mandatory-columns")
            .WithDetail("MissingColumns", missingList);

    public static ConfigError UnsupportedTypes(string info) =>
        new ConfigError($"Unsupported column types: {info}.", Section, "column-type-check")
            .WithDetail("UnsupportedColumns", info);

    public static ConfigError InvalidMaxDropdownItems(string columnKey, int value, string context) =>
        new ConfigError($"max_dropdown_items must be > 0. Actual: {value}.", Section, context)
            .WithDetail("columnKey", columnKey)
            .WithDetail("maxDropdownItems", value);

    public static ConfigError InvalidActionComboBinding(string columnKey, string? columnType, string context) =>
        new ConfigError("column_type 'action_combo_box' can only be used with key='action'.", Section, context)
            .WithDetail("columnKey", columnKey)
            .WithDetail("columnType", columnType ?? "null");
}