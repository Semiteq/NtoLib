using System.Globalization;

namespace NtoLib.MbeTable.ModuleConfig.Common;

/// <summary>
/// Invariant-culture numeric parsing abstraction.
/// </summary>
public interface INumberParser
{
	bool TryParseInt16(string text, out short value, NumberStyles styles = NumberStyles.Integer);
	bool TryParseInt32(string text, out int value, NumberStyles styles = NumberStyles.Integer);

	bool TryParseSingle(string text, out float value,
		NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands);
}
