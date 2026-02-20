using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreTimeComponentOutOfRangeError : BilingualError
{
	public CoreTimeComponentOutOfRangeError(string component, int value, int maxValue)
		: base(
			$"Invalid {component} value: {value} (must be 0-{maxValue})",
			$"Недопустимое значение {component}: {value} (должно быть 0-{maxValue})")
	{
		Component = component;
		Value = value;
		MaxValue = maxValue;
	}

	public string Component { get; }
	public int Value { get; }
	public int MaxValue { get; }
}
