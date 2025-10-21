

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;

/// <summary>
/// Represents the result of a data integrity check.
/// </summary>
public sealed record IntegrityCheckResult
{
    /// <summary>
    /// Indicates whether the integrity check passed.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Expected hash value.
    /// </summary>
    public string ExpectedHash { get; init; } = string.Empty;
    
    /// <summary>
    /// Actual calculated hash value.
    /// </summary>
    public string ActualHash { get; init; } = string.Empty;
}