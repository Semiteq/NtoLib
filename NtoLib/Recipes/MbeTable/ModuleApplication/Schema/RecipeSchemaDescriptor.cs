using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Schema;

public sealed class RecipeSchemaDescriptor : IRecipeSchemaDescriptor
{
	public RecipeSchemaDescriptor(IReadOnlyList<ColumnDefinition> columns)
	{
		if (columns == null)
		{
			throw new ArgumentNullException(nameof(columns));
		}

		TransferColumns = columns
			.Where(c => c.SaveToCsv)
			.Select(c => c.Key)
			.ToList()
			.AsReadOnly();
	}

	public IReadOnlyList<ColumnIdentifier> TransferColumns { get; }
}
