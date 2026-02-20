using System.Globalization;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Invariant-culture numeric parsing implementation.
/// </summary>
public sealed class NumberParser : INumberParser
{
	private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;

	public bool TryParseInt16(string text, out short value, NumberStyles styles = NumberStyles.Integer)
	{
		return short.TryParse(text, styles, _inv, out value);
	}

	public bool TryParseInt32(string text, out int value, NumberStyles styles = NumberStyles.Integer)
	{
		return int.TryParse(text, styles, _inv, out value);
	}

	public bool TryParseSingle(string text, out float value,
		NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands)
	{
		return float.TryParse(text, styles, _inv, out value);
	}
}
