using System;
using System.Windows.Forms;

using FluentResults;

using NtoLib.MbeTable.ServiceClipboard.Reasons.Errors;

namespace NtoLib.MbeTable.ServiceClipboard;

public sealed class SystemClipboardRawAccess : IClipboardRawAccess
{
	public string? ReadText()
	{
		try
		{
			return Clipboard.ContainsText() ? Clipboard.GetText() : null;
		}
		catch
		{
			return null;
		}
	}

	public Result WriteText(string text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		try
		{
			Clipboard.SetText(text);
			return Result.Ok();
		}
		catch (Exception ex)
		{
			return Result.Fail(new ClipboardWriteError(ex.Message));
		}
	}
}
