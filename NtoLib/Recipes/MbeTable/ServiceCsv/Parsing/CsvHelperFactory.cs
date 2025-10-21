using System.Globalization;
using System.IO;

using CsvHelper;
using CsvHelper.Configuration;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

/// <summary>
/// A factory class responsible for creating instances of <see cref="CsvReader"/> and <see cref="CsvWriter"/>
/// with predefined configurations for handling CSV data.
/// </summary>
public sealed class CsvHelperFactory : ICsvHelperFactory
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