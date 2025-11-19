using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Schema;

public interface IRecipeSchemaDescriptor
{
    IReadOnlyList<ColumnIdentifier> TransferColumns { get; }
}