using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationFilePathEmptyError : BilingualError
{
	public ApplicationFilePathEmptyError()
		: base(
			"File path is empty",
			"Путь к файлу пуст")
	{
	}
}
