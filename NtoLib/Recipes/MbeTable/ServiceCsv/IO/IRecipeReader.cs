using System.IO;

using FluentResults;

using NtoLib.Recipes.MbeTable.ServiceCsv.Data;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.IO;

/// <summary>
/// Reads raw CSV data from text input.
/// </summary>
public interface IRecipeReader
{
    Result<CsvRawData> ReadAsync(TextReader reader);
}