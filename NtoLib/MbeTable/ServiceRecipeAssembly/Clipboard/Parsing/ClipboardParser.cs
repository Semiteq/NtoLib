using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.MbeTable.ModuleCore.Entities;
using NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;
using NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Parsing;

public sealed class ClipboardParser : IClipboardParser
{
	private readonly IClipboardSchemaDescriptor _schema;
	private readonly IClipboardSchemaValidator _validator;

	public ClipboardParser(
		IClipboardSchemaDescriptor schema,
		IClipboardSchemaValidator validator)
	{
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
	}

	public Result<IReadOnlyList<PortableStepDto>> Parse(IReadOnlyList<string[]> rows)
	{
		if (rows == null)
			throw new ArgumentNullException(nameof(rows));

		var list = new List<PortableStepDto>(rows.Count);

		for (int i = 0; i < rows.Count; i++)
		{
			var rowCells = rows[i];
			var validation = _validator.ValidateRow(i, rowCells);
			if (validation.IsFailed)
				return validation.ToResult<IReadOnlyList<PortableStepDto>>();

			var actionStr = rowCells[0];
			if (!short.TryParse(actionStr, out var actionId))
				return Result.Fail<IReadOnlyList<PortableStepDto>>(new ClipboardActionIdInvalidError(i, actionStr));

			var dict = new Dictionary<string, string>(rowCells.Length - 1, StringComparer.OrdinalIgnoreCase);
			for (int c = 1; c < rowCells.Length; c++)
			{
				var key = _schema.TransferColumns[c].Value;
				dict[key] = rowCells[c];
			}

			list.Add(new PortableStepDto(actionId, dict));
		}

		return list.AsReadOnly();
	}
}
