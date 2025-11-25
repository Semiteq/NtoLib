using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpInvalidResponseError : BilingualError
{
	public object? ExpectedValue { get; }
	public object? ActualValue { get; }

	public ModbusTcpInvalidResponseError(object? expectedValue = null, object? actualValue = null)
		: base(
			BuildEnglishMessage(expectedValue, actualValue),
			BuildRussianMessage(expectedValue, actualValue))
	{
		ExpectedValue = expectedValue;
		ActualValue = actualValue;
	}

	private static string BuildEnglishMessage(object? expected, object? actual)
	{
		var msg = "Invalid response from PLC: Control register validation failed";
		if (expected != null && actual != null)
			msg += $". Expected {expected}, got {actual}";
		return msg;
	}

	private static string BuildRussianMessage(object? expected, object? actual)
	{
		var msg = "Некорректный ответ от контроллера: не пройдена проверка контрольного регистра";
		if (expected != null && actual != null)
			msg += $". Ожидалось {expected}, получено {actual}";
		return msg;
	}
}
