using System;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

namespace NtoLib.MbeTable.ModuleApplication.Schema;

public sealed class RecipeSchemaValidator : IRecipeSchemaValidator
{
	private readonly IRecipeSchemaDescriptor _schema;

	public RecipeSchemaValidator(IRecipeSchemaDescriptor schema)
	{
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
	}

	public Result ValidateRow(string[] cells)
	{
		if (cells == null || cells.Length == 0)
			return new ApplicationClipboardRowEmptyError();

		var expectedCount = _schema.TransferColumns.Count;
		if (cells.Length != expectedCount)
			return new ApplicationClipboardColumnCountMismatchError(cells.Length, expectedCount);

		var firstCell = cells[0];
		if (string.IsNullOrWhiteSpace(firstCell))
			return new ApplicationClipboardActionIdMissingError();

		if (!short.TryParse(firstCell, out _))
			return new ApplicationClipboardActionIdInvalidError(firstCell);

		return Result.Ok();
	}
}
