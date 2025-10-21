namespace NtoLib.Recipes.MbeTable.ModulePresentation.Models;

/// <summary>
/// Represents the execution phase of a table row.
/// Higher priority than CellDataState for visual styling.
/// </summary>
public enum RowExecutionState
{
    /// <summary>
    /// Row is scheduled to execute in the future.
    /// </summary>
    Upcoming,

    /// <summary>
    /// Row is currently executing.
    /// </summary>
    Current,

    /// <summary>
    /// Row has already been executed.
    /// </summary>
    Passed
}