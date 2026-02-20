using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CorePropertyParsingFailedError : BilingualError
{
	public CorePropertyParsingFailedError(string defaultValue, string columnKey)
		: base(
			$"Failed to parse default value '{defaultValue}' for column '{columnKey}'",
			$"Не удалось разобрать значение по умолчанию '{defaultValue}' для столбца '{columnKey}'")
	{
		DefaultValue = defaultValue;
		ColumnKey = columnKey;
	}

	public string DefaultValue { get; }
	public string ColumnKey { get; }
}
