#nullable enable

using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

/// <summary>
/// Represents metadata for a recipe file, including signature, version information,
/// separator details, row count, body hash, and any extra data.
/// </summary>
public sealed record RecipeFileMetadata
{
    /// <summary>
    /// Metadata format version
    /// </summary>
    public string Signature { get; init; } = "MBE-RECIPE";

    /// <summary>
    /// Represents the version of the recipe file metadata
    /// </summary>
    public int Version { get; init; } = 1;
    
    /// <summary>
    /// CSV separator
    /// </summary>
    public char Separator { get; init; } = ';';

    /// <summary>
    /// Total number of data rows
    /// </summary>
    public int Rows { get; init; } = 0;
    
    /// <summary>
    /// SHA-256 hash of normalized data rows
    /// </summary>
    public string BodyHashBase64 { get; init; } = "";

    /// <summary>
    /// Additional metadata for extensibility
    /// </summary>
    public Dictionary<string, string> Extras { get; init; } = new();
}