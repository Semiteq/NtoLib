using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Errors;

public sealed class AssemblyMissingPlcMappingError : BilingualError
{
    public string ColumnName { get; }

    public AssemblyMissingPlcMappingError(string columnName)
        : base(
            $"{columnName} column has no PLC mapping",
            $"Столбец {columnName} не имеет отображения PLC")
    {
        ColumnName = columnName;
        Metadata["columnName"] = columnName;
    }
}