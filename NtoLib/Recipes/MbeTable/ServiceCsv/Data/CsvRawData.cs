using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Data;

/// <summary>
/// Represents raw CSV data extracted from a file.
/// </summary>
public sealed class CsvRawData
{
    /// <summary>
    /// CSV column headers.
    /// </summary>
    public IReadOnlyList<string> Headers { get; set; } = new List<string>();
    
    /// <summary>
    /// CSV data rows as strings (canonical format).
    /// </summary>
    public IReadOnlyList<string> Rows { get; set; } = new List<string>();
    
    /// <summary>
    /// Parsed CSV records (row arrays).
    /// </summary>
    public IReadOnlyList<string[]> Records { get; set; } = new List<string[]>();
    
    /// <summary>
    /// File metadata if available.
    /// </summary>
    public RecipeFileMetadata? Metadata { get; set; }
}