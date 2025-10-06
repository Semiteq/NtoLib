

using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Services;

namespace NtoLib.Recipes.MbeTable.Csv.Parsing;

public interface ICsvHeaderBinder
{
    /// <summary>
    /// Binds the header tokens from a CSV file to the provided table schema.
    /// </summary>
    Result<CsvHeaderBinder.Binding> Bind(string[] headerTokens, TableColumns columns);
}