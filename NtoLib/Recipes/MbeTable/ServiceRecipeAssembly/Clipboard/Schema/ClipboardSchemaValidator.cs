using System;

using FluentResults;

using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

public sealed class ClipboardSchemaValidator
{
	private readonly ClipboardSchemaDescriptor _schema;

	public ClipboardSchemaValidator(ClipboardSchemaDescriptor schema)
	{
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
	}

	public Result ValidateRow(int rowIndex, string[] cells)
	{
		if (cells.Length == 0)
		{
			return new ClipboardRowEmptyError(rowIndex);
		}

		var expected = _schema.TransferColumns.Count;
		if (cells.Length != expected)
		{
			return new ClipboardColumnCountMismatchError(rowIndex, cells.Length, expected);
		}

		var first = cells[0];
		if (string.IsNullOrWhiteSpace(first))
		{
			return new ClipboardActionIdMissingError(rowIndex);
		}

		if (!short.TryParse(first, out _))
		{
			return new ClipboardActionIdInvalidError(rowIndex, first);
		}

		return Result.Ok();
	}
}
