using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CorePropertyTypeMismatchError : BilingualError
{
	public CorePropertyTypeMismatchError(string expectedType, string actualType)
		: base(
			$"Property type mismatch: expected {expectedType}, got {actualType}",
			$"Несоответствие типа свойства: ожидается {expectedType}, получен {actualType}")
	{
		ExpectedType = expectedType;
		ActualType = actualType;
	}

	public string ExpectedType { get; }
	public string ActualType { get; }
}
