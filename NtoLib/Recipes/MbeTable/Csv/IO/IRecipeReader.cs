

using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Csv.Data;

namespace NtoLib.Recipes.MbeTable.Csv.IO;

/// <summary>
/// Reads raw CSV data from text input.
/// </summary>
public interface IRecipeReader
{
    Result<CsvRawData> ReadAsync(TextReader reader);
}