using System.Collections.Generic;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.MbeTable.ModuleApplication.Schema;

public interface IRecipeSchemaDescriptor
{
	IReadOnlyList<ColumnIdentifier> TransferColumns { get; }
}
