using System.Text;
using System.Text.RegularExpressions;

namespace NtoLib.Recipes.MbeTable.ServiceClipboard.Sanitizer;

public static class ClipboardSanitizer
{
	private const int MaxCellLength = 2000;
	private static readonly Regex _unsafeRegex = new(@"[\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

	public static string SanitizeForCell(string? input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return string.Empty;
		}

		var normalized = input!
			.Replace('\t', ' ')
			.Replace('\r', ' ')
			.Replace('\n', ' ');

		// Remove remaining unsafe control characters
		normalized = _unsafeRegex.Replace(normalized, string.Empty);

		// Trim and collapse multiple spaces
		normalized = CollapseSpaces(normalized.Trim());

		// Enforce length limit
		if (normalized.Length > MaxCellLength)
		{
			normalized = normalized.Substring(0, MaxCellLength);
		}

		return normalized;
	}

	private static string CollapseSpaces(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		var sb = new StringBuilder(s.Length);
		var previousSpace = false;
		foreach (var ch in s)
		{
			if (char.IsWhiteSpace(ch))
			{
				if (!previousSpace)
				{
					sb.Append(' ');
					previousSpace = true;
				}
			}
			else
			{
				sb.Append(ch);
				previousSpace = false;
			}
		}

		return sb.ToString();
	}
}
