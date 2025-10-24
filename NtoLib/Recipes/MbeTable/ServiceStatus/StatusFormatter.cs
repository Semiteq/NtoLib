using System.Text;

namespace NtoLib.Recipes.MbeTable.ServiceStatus;

/// <summary>
/// Default formatter that trims and optionally limits message length.
/// </summary>
public class StatusFormatter
{
    private readonly int? _maxLength;

    public StatusFormatter(int? maxLength = 100)
    {
        _maxLength = maxLength;
        if (maxLength is <= 0) _maxLength = null;
    }

    public string Format(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return string.Empty;

        var trimmed = message.Trim();

        if (_maxLength is null || trimmed.Length <= _maxLength.Value)
            return trimmed;

        var sb = new StringBuilder(_maxLength.Value + 3);
        sb.Append(trimmed.Substring(0, _maxLength.Value));
        sb.Append("...");
        return sb.ToString();
    }
}