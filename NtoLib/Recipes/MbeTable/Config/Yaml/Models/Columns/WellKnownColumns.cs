#nullable enable

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

public static class WellKnownColumns
{
    public static ColumnIdentifier Action { get; } = new("action");
    public static ColumnIdentifier StepDuration { get; } = new("step-duration");
    public static ColumnIdentifier StepStartTime { get; } = new("step-start-time");
    public static ColumnIdentifier Comment { get; } = new("comment");
}