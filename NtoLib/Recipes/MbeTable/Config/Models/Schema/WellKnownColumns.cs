#nullable enable

namespace NtoLib.Recipes.MbeTable.Config.Models.Schema;

/// <summary>
/// Provides static, well-known instances of <see cref="ColumnIdentifier"/> for system-critical columns.
/// Using these properties ensures compile-time safety and avoids "magic strings" in the code.
/// </summary>
public static class WellKnownColumns
{
    /// <summary>
    /// Identifier for the column that defines the step's action.
    /// </summary>
    public static ColumnIdentifier Action { get; } = new("action");

    /// <summary>
    /// Identifier for the column that specifies the target of the action (e.g., a specific heater or shutter).
    /// </summary>
    public static ColumnIdentifier ActionTarget { get; } = new("action-target");

    /// <summary>
    /// Identifier for the column representing the initial value for an operation (e.g., starting temperature).
    /// </summary>
    public static ColumnIdentifier InitialValue { get; } = new("initial-value");

    /// <summary>
    /// Identifier for the column representing the target setpoint for an operation.
    /// </summary>
    public static ColumnIdentifier Setpoint { get; } = new("setpoint");

    /// <summary>
    /// Identifier for the column that defines the rate of change (e.g., temperature ramp speed).
    /// </summary>
    public static ColumnIdentifier Speed { get; } = new("speed");

    /// <summary>
    /// Identifier for the column that specifies the duration of a step.
    /// </summary>
    public static ColumnIdentifier StepDuration { get; } = new("step-duration");

    /// <summary>
    /// Identifier for the column showing the calculated start time of a step. This is a read-only column.
    /// </summary>
    public static ColumnIdentifier StepStartTime { get; } = new("step-start-time");

    /// <summary>
    /// Identifier for the column containing user comments. This is considered a non-critical column.
    /// </summary>
    public static ColumnIdentifier Comment { get; } = new("comment");
}