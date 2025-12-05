using System.Collections.Generic;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

public interface IClipboardSchemaDescriptor
{
	IReadOnlyList<ColumnIdentifier> TransferColumns { get; }
}
