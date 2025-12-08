using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;
using NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.ComboBox;

public sealed class TargetComboBox : FactoryColumnComboBoxBase
{
	public TargetComboBox(IServiceProvider serviceProvider, IColumnAlignmentResolver alignmentResolver)
		: base(serviceProvider, alignmentResolver)
	{
	}

	protected override IList<KeyValuePair<short, string>> GetDataSource() =>
		new List<KeyValuePair<short, string>>();

	protected override void AssignItemsProvider(RecipeComboBoxCell cell) =>
		cell.SetItemsProvider(ServiceProvider.GetRequiredService<TargetItemsProvider>());
}
