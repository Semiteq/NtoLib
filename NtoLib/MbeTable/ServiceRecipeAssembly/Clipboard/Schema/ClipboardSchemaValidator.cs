using System;

using FluentResults;

using NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

public sealed class ClipboardSchemaValidator : IClipboardSchemaValidator
{
	private readonly IClipboardSchemaDescriptor _schema;

	public ClipboardSchemaValidator(IClipboardSchemaDescriptor schema)
	{
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
	}

	public Result ValidateRow(int rowIndex, string[] cells)
	{
		if (cells.Length == 0)
			return new ClipboardRowEmptyError(rowIndex);

		var expected = _schema.TransferColumns.Count;
		if (cells.Length != expected)
			return new ClipboardColumnCountMismatchError(rowIndex, cells.Length, expected);

		var first = cells[0];
		if (string.IsNullOrWhiteSpace(first))
			return new ClipboardActionIdMissingError(rowIndex);

		if (!short.TryParse(first, out _))
			return new ClipboardActionIdInvalidError(rowIndex, first);

		return Result.Ok();
	}
}
