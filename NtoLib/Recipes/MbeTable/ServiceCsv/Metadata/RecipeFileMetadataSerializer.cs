using System;
using System.Collections.Generic;
using System.IO;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

/// <summary>
/// Handles serialization and deserialization of metadata related to recipe files.
/// </summary>
public class RecipeFileMetadataSerializer
{
    public void Write(TextWriter writer, RecipeFileMetadata meta)
    {
        writer.WriteLine($"# SEP=\"{meta.Separator}\"");
        writer.WriteLine($"# ROWS=\"{meta.Rows}\"");
        writer.WriteLine($"# BODY_SHA256=\"{meta.BodyHashBase64}\"");
        foreach (var kv in meta.Extras)
            writer.WriteLine($"# X_{kv.Key}=\"{kv.Value}\"");
    }

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
            var rawValue = payload.Substring(eq + 1);
            var value = UnquoteValue(rawValue);

            switch (key.ToUpperInvariant())
            {
                case "SEP":
                    meta = meta with { Separator = value.Length > 0 ? value[0] : ';' };
                    break;
                case "ROWS":
                    if (int.TryParse(value, out var rows))
                        meta = meta with { Rows = rows };
                    break;
                case "BODY_SHA256":
                    meta = meta with { BodyHashBase64 = value };
                    break;
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

    private static string UnquoteValue(string value)
    {
        var trimmed = value.Trim();
    
        if (trimmed.Length >= 2 && 
            trimmed[0] == '"' && 
            trimmed[^1] == '"')
        {
            return trimmed.Substring(1, trimmed.Length - 2);
        }
    
        return trimmed;
    }

}