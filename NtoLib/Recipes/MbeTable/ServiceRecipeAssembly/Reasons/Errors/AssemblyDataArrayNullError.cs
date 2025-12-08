using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyDataArrayNullError : BilingualError
{
	public string ArrayName { get; }

	public AssemblyDataArrayNullError(string arrayName)
		: base(
			$"{arrayName} data array is null",
			$"Массив данных {arrayName} равен null")
	{
		ArrayName = arrayName;
		Metadata["arrayName"] = arrayName;
	}
}
