using System;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

// Error type for formula precompilation failures.
public sealed class ConfigFormulaCompilationError : Error
{
	public ConfigFormulaCompilationError(
		string message,
		short actionId,
		string actionName,
		string expression,
		Exception? cause = null)
		: base(message)
	{
		ActionId = actionId;
		ActionName = actionName;
		Expression = expression;

		WithMetadata("actionId", ActionId.ToString());
		WithMetadata("actionName", ActionName);
		WithMetadata("expression", Expression);

		if (cause != null)
		{
			CausedBy(cause);
		}
	}

	public short ActionId { get; }
	public string ActionName { get; }
	public string Expression { get; }
}
