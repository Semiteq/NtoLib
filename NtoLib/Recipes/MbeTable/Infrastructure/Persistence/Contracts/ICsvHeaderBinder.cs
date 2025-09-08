#nullable enable

using FluentResults;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

public interface ICsvHeaderBinder
{
    /// <summary>
    /// Binds the header tokens from a CSV file to the provided table schema.
    /// </summary>
    /// <param name="headerTokens">An array of strings representing the header columns from the CSV file.</param>
    /// <param name="schema">The table schema to bind against.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the binding information on success, or an error on failure.
    /// </returns>
    Result<CsvHeaderBinder.Binding> Bind(string[] headerTokens, TableSchema schema);
}