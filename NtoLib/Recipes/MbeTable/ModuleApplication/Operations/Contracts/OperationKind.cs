namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

/// <summary>
/// Types of long-running operations that can block UI.
/// </summary>
public enum OperationKind
{
    Loading,
    Saving,
    Transferring,
    Other
}