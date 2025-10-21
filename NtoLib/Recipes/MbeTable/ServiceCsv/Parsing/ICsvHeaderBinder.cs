using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

public interface ICsvHeaderBinder
{
    /// <summary>
    /// Binds the header tokens from a CSV file to the provided table schema.
    /// </summary>
    Result<CsvHeaderBinder.Binding> Bind(string[] headerTokens, TableColumns columns);
}