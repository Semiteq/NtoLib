using System.IO;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Data;

/// <summary>
/// Extracts raw data from CSV format.
/// </summary>
public interface ICsvDataExtractor
{
	/// <summary>
	/// Extracts raw CSV data from the provided reader.
	/// </summary>
	/// <param name="reader">Text reader containing CSV data.</param>
	/// <returns>Result containing raw CSV data or error information.</returns>
	Result<CsvRawData> ExtractRawData(TextReader reader);
}
