using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationUnexpectedIoReadError : BilingualError
{
	public ApplicationUnexpectedIoReadError()
		: base(
			"Unexpected error during read operation",
			"Непредвиденная ошибка при чтении")
	{
	}
}
