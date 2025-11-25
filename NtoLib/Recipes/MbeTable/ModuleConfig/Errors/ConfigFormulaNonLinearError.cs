using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaNonLinearError : Error
{
	public ConfigFormulaNonLinearError()
		: base("Formula is non-linear (variable appears more than once)")
	{
	}
}
