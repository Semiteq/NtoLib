namespace NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;

public sealed record IntegrityCheckResult
{
	public bool IsValid { get; init; }
	public string ExpectedHash { get; init; } = string.Empty;
	public string ActualHash { get; init; } = string.Empty;
}
