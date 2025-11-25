using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationClipboardActionIdInvalidError : BilingualError
{
	public string Value { get; }

	public ApplicationClipboardActionIdInvalidError(string value)
		: base(
			$"Invalid action ID in clipboard: '{value}'",
			$"Недопустимый ID действия в буфере обмена: '{value}'")
	{
		Value = value;
	}
}
