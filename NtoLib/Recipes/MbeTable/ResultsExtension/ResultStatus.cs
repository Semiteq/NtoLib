namespace NtoLib.Recipes.MbeTable.ResultsExtension;

/// <summary>
/// Represents the semantic status of a Result after operation execution.
/// </summary>
public enum ResultStatus
{
    /// <summary>
    /// Operation succeeded without any issues.
    /// </summary>
    Success,
    
    /// <summary>
    /// Operation succeeded but data has validation issues or warnings.
    /// </summary>
    Warning,
    
    /// <summary>
    /// Operation failed to execute.
    /// </summary>
    Failure
}