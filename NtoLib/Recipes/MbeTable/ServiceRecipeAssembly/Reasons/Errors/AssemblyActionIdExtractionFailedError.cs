using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyActionIdExtractionFailedError : BilingualError
{
	public AssemblyActionIdExtractionFailedError(int? lineNumber = null)
		: base(
			lineNumber.HasValue
				? $"Failed to extract action ID at line {lineNumber.Value}"
				: "Failed to extract action ID",
			lineNumber.HasValue
				? $"Не удалось извлечь ID действия на строке {lineNumber.Value}"
				: "Не удалось извлечь ID действия")
	{
		LineNumber = lineNumber;
		if (lineNumber.HasValue)
		{
			Metadata["lineNumber"] = lineNumber.Value;
		}
	}

	public int? LineNumber { get; }
}
