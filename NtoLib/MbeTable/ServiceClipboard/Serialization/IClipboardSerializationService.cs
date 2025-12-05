using System.Collections.Generic;

using FluentResults;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ServiceClipboard.Serialization;

public interface IClipboardSerializationService
{
	string SerializeSteps(IReadOnlyList<Step> steps, IReadOnlyList<ColumnIdentifier> columns);
	Result<IReadOnlyList<string[]>> SplitRows(string? tsv);
}
