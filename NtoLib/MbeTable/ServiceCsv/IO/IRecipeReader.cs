using System.IO;

using FluentResults;

using NtoLib.MbeTable.ServiceCsv.Data;

namespace NtoLib.MbeTable.ServiceCsv.IO;

/// <summary>
/// Reads raw CSV data from text input.
/// </summary>
public interface IRecipeReader
{
	Result<CsvRawData> ReadAsync(TextReader reader);
}
