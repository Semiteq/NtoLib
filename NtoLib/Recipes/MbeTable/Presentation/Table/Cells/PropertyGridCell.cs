#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

/// <summary>
/// DataGridView cell for numeric property columns that lets the user type a number followed by arbitrary unit text.
/// Only the leading numeric token (with optional sign and one decimal separator) is parsed and committed.
/// The unit suffix is ignored on input. Display (value + units) is produced elsewhere (domain / view model).
/// </summary>
public sealed class PropertyGridCell : NonSelectableCellBase
{
    /// <summary>
    /// Parses user-entered text, extracting only the leading numeric part (including an optional sign)
    /// and converting it to the column's ValueType (int or float). Unit suffix is ignored.
    /// </summary>
    public override object? ParseFormattedValue(
        object? formattedValue,
        DataGridViewCellStyle cellStyle,
        TypeConverter? formattedValueTypeConverter,
        TypeConverter? valueTypeConverter)
    {
        if (formattedValue == null)
            return null;

        var targetType = OwningColumn?.ValueType;
        if (targetType != typeof(int) && targetType != typeof(float))
        {
            return base.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter);
        }

        var text = formattedValue.ToString() ?? string.Empty;
        var token = ExtractLeadingNumericToken(text);
        if (string.IsNullOrEmpty(token))
            throw new FormatException("Invalid numeric value.");

        if (targetType == typeof(int))
        {
            if (TryParseInt(token, out var intValue))
                return intValue;
            throw new FormatException("Invalid integer value.");
        }

        if (targetType == typeof(float))
        {
            if (TryParseFloat(token, out var floatValue))
                return floatValue;
            throw new FormatException("Invalid float value.");
        }

        return base.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter);
    }

    private static string ExtractLeadingNumericToken(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        int i = 0;
        int length = input.Length;

        while (i < length && char.IsWhiteSpace(input[i]))
            i++;

        if (i >= length)
            return string.Empty;

        int start = i;

        if (input[i] == '+' || input[i] == '-')
            i++;

        bool hasDigits = false;
        bool hasSeparator = false;

        while (i < length)
        {
            char c = input[i];
            if (char.IsDigit(c))
            {
                hasDigits = true;
                i++;
                continue;
            }

            if ((c == '.' || c == ',') && !hasSeparator)
            {
                hasSeparator = true;
                i++;
                continue;
            }

            break;
        }

        if (!hasDigits)
            return string.Empty;

        return input.Substring(start, i - start);
    }

    private static bool TryParseInt(string token, out int value)
    {
        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            return true;
        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;
        return false;
    }

    private static bool TryParseFloat(string token, out float value)
    {
        if (float.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value))
            return true;
        if (float.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
            return true;
        return false;
    }
}