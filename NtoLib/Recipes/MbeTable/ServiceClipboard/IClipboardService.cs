using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceClipboard;

public interface IClipboardService
{
	Result WriteSteps(IReadOnlyList<Step> steps, IReadOnlyList<ColumnIdentifier> columns);
	Result<IReadOnlyList<string[]>> ReadRows();
}
