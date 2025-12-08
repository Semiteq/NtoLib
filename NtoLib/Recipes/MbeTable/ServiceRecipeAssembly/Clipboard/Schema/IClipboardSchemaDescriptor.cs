using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

public interface IClipboardSchemaDescriptor
{
	IReadOnlyList<ColumnIdentifier> TransferColumns { get; }
}
