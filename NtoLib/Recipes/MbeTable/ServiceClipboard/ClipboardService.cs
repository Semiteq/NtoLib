using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceClipboard.Serialization;

namespace NtoLib.Recipes.MbeTable.ServiceClipboard;

public sealed class ClipboardService
{
	private readonly IClipboardRawAccess _rawAccess;
	private readonly ClipboardSerializationService _serialization;

	public ClipboardService(
		IClipboardRawAccess rawAccess,
		ClipboardSerializationService serialization)
	{
		_rawAccess = rawAccess ?? throw new ArgumentNullException(nameof(rawAccess));
		_serialization = serialization ?? throw new ArgumentNullException(nameof(serialization));
	}

	public Result WriteSteps(IReadOnlyList<Step> steps, IReadOnlyList<ColumnIdentifier> columns)
	{
		var tsv = _serialization.SerializeSteps(steps, columns);

		return _rawAccess.WriteText(tsv);
	}

	public Result<IReadOnlyList<string[]>> ReadRows()
	{
		var text = _rawAccess.ReadText();

		return _serialization.SplitRows(text);
	}
}
