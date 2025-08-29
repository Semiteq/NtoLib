#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

/// <summary>
/// Handles serialization and deserialization of metadata related to recipe files.
/// This facilitates reading and writing metadata for the recipe file format,
/// including versioning, separators, fingerprints, row counts, and additional metadata extras.
/// </summary>
public class RecipeFileMetadataSerializer
{


    public void Write(TextWriter writer, RecipeFileMetadata meta)
    {
        writer.WriteLine($"# SEP={meta.Separator}");
        writer.WriteLine($"# ROWS={meta.Rows}");
        writer.WriteLine($"# BODY_SHA256={meta.BodyHashBase64}");
        foreach (var kv in meta.Extras)
            writer.WriteLine($"# X_{kv.Key}={kv.Value}");
    }

    /// <summary>
    /// Parses the full text of a recipe file to extract metadata, the number of metadata lines,
    /// and a flag indicating if the signature line (e.g., "# MBE-RECIPE v=...") was found.
    /// </summary>
    /// <param name="fullText">The full-text content of the recipe file to be parsed.</param>
    /// <returns>
    /// A tuple containing:
    /// - <c>Meta</c>: The parsed metadata as a <see cref="RecipeFileMetadata"/> object.
    /// - <c>MetaLines</c>: The count of metadata lines parsed from the text.
    /// - <c>SignatureFound</c>: A boolean indicating if the signature line was detected.
    /// </returns>
    public (RecipeFileMetadata Meta, int MetaLines) ReadAllMeta(string fullText)
    {
        var reader = new StringReader(fullText);
        var meta = new RecipeFileMetadata();
        var extras = new Dictionary<string, string>();
        var lines = 0;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!line.StartsWith("#")) break;
            lines++;

            var payload = line.TrimStart('#', ' ');

            var eq = payload.IndexOf('=');
            if (eq <= 0) continue;

            var key = payload.Substring(0, eq).Trim();
            var value = payload.Substring(eq + 1);

            switch (key.ToUpperInvariant())
            {
                case "SEP": meta = meta with { Separator = value.Length > 0 ? value[0] : ';' }; break;
                case "ROWS": if (int.TryParse(value, out var rows)) meta = meta with { Rows = rows }; break;
                case "BODY_SHA256": meta = meta with { BodyHashBase64 = value }; break;
                default:
                    if (key.StartsWith("X_", StringComparison.OrdinalIgnoreCase))
                        extras[key.Substring(2)] = value;
                    break;
            }
        }

        foreach (var kv in extras)
            meta.Extras[kv.Key] = kv.Value;

        return (meta, lines);
    }
}