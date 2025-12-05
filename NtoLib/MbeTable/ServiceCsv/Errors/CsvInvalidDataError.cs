using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Errors;

public sealed class CsvInvalidDataError : BilingualError
{
	public string Reason { get; }
	public int? LineNumber { get; }

	public CsvInvalidDataError(string reason, int? lineNumber = null)
		: base(
			lineNumber.HasValue
				? $"Invalid CSV data at line {lineNumber.Value}: {reason}"
				: $"Invalid CSV data: {reason}",
			lineNumber.HasValue
				? $"Некорректные данные CSV на строке {lineNumber.Value}: {reason}"
				: $"Некорректные данные CSV: {reason}")
	{
		Reason = reason;
		LineNumber = lineNumber;
	}
}
