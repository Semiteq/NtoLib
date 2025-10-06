

using System.Globalization;
using System.IO;
using CsvHelper;

namespace NtoLib.Recipes.MbeTable.Csv.Parsing;

public interface ICsvHelperFactory
{
    CultureInfo Culture { get; }

    /// <summary>
    /// Creates and returns a new instance of <see cref="CsvReader"/> using the provided <see cref="TextReader"/> and pre-configured settings.
    /// </summary>
    CsvReader CreateReader(TextReader reader);

    /// <summary>
    /// Creates and returns a new instance of <see cref="CsvWriter"/> using the provided <see cref="TextWriter"/> and pre-configured settings.
    /// </summary>
    CsvWriter CreateWriter(TextWriter writer);
}