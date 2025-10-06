namespace NtoLib.Recipes.MbeTable.Application.State;

/// <summary>
/// Types of long-running operations that can block UI.
/// </summary>
public enum OperationKind
{
    Loading,
    Saving,
    Transferring,
    None
}