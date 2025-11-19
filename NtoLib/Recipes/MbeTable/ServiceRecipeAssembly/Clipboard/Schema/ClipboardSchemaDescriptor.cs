using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

public sealed class ClipboardSchemaDescriptor : IClipboardSchemaDescriptor
{
    public IReadOnlyList<ColumnIdentifier> TransferColumns { get; }

    public ClipboardSchemaDescriptor(IReadOnlyList<ColumnDefinition> columns)
    {
        TransferColumns = columns
            .Where(c => c.SaveToCsv)
            .Select(c => c.Key)
            .ToList()
            .AsReadOnly();
    }
}