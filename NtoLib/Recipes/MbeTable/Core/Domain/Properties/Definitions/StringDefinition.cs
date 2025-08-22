using System;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class StringDefinition : IPropertyTypeDefinition
{
    public string Units => "";
    public Type SystemType => typeof(string);

    public (bool Success, string errorMessage) Validate(object value)
    {
        if (value.ToString().Length is < 0 or > 255)
            return (false, "Длина строки должна быть от 0 до 255 символов");

        return (true, "");
    }

    public string FormatValue(object value) => (string)value;

    public (bool Success, object Value) TryParse(string input)
    {
        var s = input ?? string.Empty;
        s = s.Trim();

        // Normalize line endings to LF
        if (s.IndexOf('\r') >= 0)
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");

        // Remove control chars except LF and TAB
        {
            var chars = s.AsSpan();
            var buffer = new char[chars.Length];
            var idx = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (char.IsControl(c) && c != '\n' && c != '\t')
                    continue;
                buffer[idx++] = c;
            }

            s = new string(buffer, 0, idx);
        }

        // Escape quotes
        if (s.Contains('"'))
            s = s.Replace("\"", "\"\"");

        // Quote if contains delimiters
        if (s.IndexOfAny(new[] { ',', ';', '\n', '"' }) >= 0)
            s = $"\"{s}\"";

        return (true, s);
    }
}