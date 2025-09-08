#nullable enable

using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

/// <summary>
/// A factory class responsible for creating instances of <see cref="CsvReader"/> and <see cref="CsvWriter"/>
/// with predefined configurations for handling CSV data.
/// </summary>
public sealed class CsvHelperFactory : ICsvHelperFactory
{
    public char Separator { get; }
    public CultureInfo Culture { get; init; }
    
    public CsvHelperFactory(CultureInfo? culture = null, char separator = ';')
    {
        Culture = culture ?? CultureInfo.InvariantCulture;
        Separator = separator;
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="CsvReader"/> using the provided <see cref="TextReader"/> and pre-configured settings.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader"/> to read CSV data from.</param>
    /// <returns>A configured <see cref="CsvReader"/> instance.</returns>
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

    /// <summary>
    /// Creates and returns a new instance of <see cref="CsvWriter"/> using the provided <see cref="TextWriter"/> and pre-configured settings.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> to write CSV data to.</param>
    /// <returns>A configured <see cref="CsvWriter"/> instance.</returns>
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