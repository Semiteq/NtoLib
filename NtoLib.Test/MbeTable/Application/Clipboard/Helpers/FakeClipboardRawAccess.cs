using FluentResults;

using NtoLib.Recipes.MbeTable.ServiceClipboard;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

public sealed class FakeClipboardRawAccess : IClipboardRawAccess
{
    public string WrittenText { get; private set; } = string.Empty;

    public string ReadText()
    {
        return WrittenText;
    }

    public Result WriteText(string text)
    {
        WrittenText = text;
        return Result.Ok();
    }

    public void SetText(string text)
    {
        WrittenText = text;
    }

    public void Clear()
    {
        WrittenText = string.Empty;
    }
}