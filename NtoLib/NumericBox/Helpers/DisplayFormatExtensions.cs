namespace NtoLib.NumericBox.Helpers;

public static class DisplayFormatExtensions
{
	public static string ToFormatString(this DisplayFormat format)
	{
		return format switch
		{
			DisplayFormat.Integer => "0",
			DisplayFormat.OneDecimal => "0.#",
			DisplayFormat.TwoDecimals => "0.##",
			DisplayFormat.ThreeDecimals => "0.###",
			_ => "0.##"
		};
	}
}
