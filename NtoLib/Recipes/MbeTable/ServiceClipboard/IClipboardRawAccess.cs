using FluentResults;

namespace NtoLib.Recipes.MbeTable.ServiceClipboard;

public interface IClipboardRawAccess
{
    string? ReadText();
    Result WriteText(string text);
}