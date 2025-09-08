using System.Globalization;
using System.IO;
using CsvHelper;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

public interface ICsvHelperFactory
{
    char Separator { get; }
    CultureInfo Culture { get; init; }

    /// <summary>
    /// Creates and returns a new instance of <see cref="CsvReader"/> using the provided <see cref="TextReader"/> and pre-configured settings.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader"/> to read CSV data from.</param>
    /// <returns>A configured <see cref="CsvReader"/> instance.</returns>
    CsvReader CreateReader(TextReader reader);

    /// <summary>
    /// Creates and returns a new instance of <see cref="CsvWriter"/> using the provided <see cref="TextWriter"/> and pre-configured settings.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> to write CSV data to.</param>
    /// <returns>A configured <see cref="CsvWriter"/> instance.</returns>
    CsvWriter CreateWriter(TextWriter writer);
}