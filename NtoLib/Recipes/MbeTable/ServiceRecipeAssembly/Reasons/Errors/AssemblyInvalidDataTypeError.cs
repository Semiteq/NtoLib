using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyInvalidDataTypeError : BilingualError
{
	public AssemblyInvalidDataTypeError(string expectedType, string actualType)
		: base(
			$"Invalid data type: expected {expectedType}, got {actualType}",
			$"Неверный тип данных: ожидается {expectedType}, получен {actualType}")
	{
		ExpectedType = expectedType;
		ActualType = actualType;
		Metadata["expectedType"] = expectedType;
		Metadata["actualType"] = actualType;
	}

	public string ExpectedType { get; }
	public string ActualType { get; }
}
