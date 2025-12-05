using FluentResults;

namespace NtoLib.MbeTable.ServiceClipboard;

public interface IClipboardRawAccess
{
	string? ReadText();
	Result WriteText(string text);
}
