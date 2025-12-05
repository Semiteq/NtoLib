using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.MbeTable.ModuleApplication.Schema;

public sealed class RecipeSchemaDescriptor : IRecipeSchemaDescriptor
{
	public IReadOnlyList<ColumnIdentifier> TransferColumns { get; }

	public RecipeSchemaDescriptor(IReadOnlyList<ColumnDefinition> columns)
	{
		if (columns == null)
			throw new ArgumentNullException(nameof(columns));

		TransferColumns = columns
			.Where(c => c.SaveToCsv)
			.Select(c => c.Key)
			.ToList()
			.AsReadOnly();
	}
}
