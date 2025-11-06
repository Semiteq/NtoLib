using System.Globalization;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Invariant-culture numeric parsing implementation.
/// </summary>
public sealed class NumberParser : INumberParser
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public bool TryParseInt16(string text, out short value, NumberStyles styles = NumberStyles.Integer)
        => short.TryParse(text, styles, Inv, out value);

    public bool TryParseInt32(string text, out int value, NumberStyles styles = NumberStyles.Integer)
        => int.TryParse(text, styles, Inv, out value);

    public bool TryParseSingle(string text, out float value,
        NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands)
        => float.TryParse(text, styles, Inv, out value);
}