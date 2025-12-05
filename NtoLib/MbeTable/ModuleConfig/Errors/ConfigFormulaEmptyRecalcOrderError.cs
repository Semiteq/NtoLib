using FluentResults;

namespace NtoLib.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaEmptyRecalcOrderError : Error
{
	public ConfigFormulaEmptyRecalcOrderError()
		: base("Recalculation order is empty")
	{
	}
}
