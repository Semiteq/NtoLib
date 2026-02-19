using System.Globalization;
using System.IO;

using CsvHelper;
using CsvHelper.Configuration;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

public sealed class CsvHelperFactory
{
	private const char Separator = ';';

	public CultureInfo Culture { get; }

	public CsvHelperFactory(CultureInfo? culture = null)
	{
		Culture = culture ?? CultureInfo.InvariantCulture;
	}

	public CsvReader CreateReader(TextReader reader)
	{
		var cfg = new CsvConfiguration(Culture)
		{
			HasHeaderRecord = true,
			Delimiter = Separator.ToString(),
			BadDataFound = null,
			MissingFieldFound = null,
			TrimOptions = TrimOptions.Trim
		};
		return new CsvReader(reader, cfg);
	}

	public CsvWriter CreateWriter(TextWriter writer)
	{
		writer.NewLine = "\r\n";
		var cfg = new CsvConfiguration(Culture)
		{
			HasHeaderRecord = true,
			Delimiter = Separator.ToString()
		};
		return new CsvWriter(writer, cfg);
	}
}
