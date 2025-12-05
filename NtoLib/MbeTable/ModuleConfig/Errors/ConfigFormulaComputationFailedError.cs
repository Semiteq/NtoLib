using FluentResults;

namespace NtoLib.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaComputationFailedError : Error
{
	public string Details { get; }

	public ConfigFormulaComputationFailedError(string details)
		: base($"Formula computation failed: {details}")
	{
		Details = details;
		WithMetadata("details", details);
	}
}
