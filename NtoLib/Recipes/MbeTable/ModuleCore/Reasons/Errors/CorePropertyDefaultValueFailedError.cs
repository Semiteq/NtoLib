using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CorePropertyDefaultValueFailedError : BilingualError
{
    public string ColumnValue { get; }

    public CorePropertyDefaultValueFailedError(string columnValue)
        : base(
            $"Failed to set default value for column '{columnValue}'",
            $"Не удалось установить значение по умолчанию для столбца '{columnValue}'")
    {
        ColumnValue = columnValue;
    }
}