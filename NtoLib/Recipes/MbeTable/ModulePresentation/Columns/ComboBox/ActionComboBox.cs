using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;
using NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.ComboBox;

public sealed class ActionComboBox : FactoryColumnComboBoxBase
{
	private readonly IComboboxDataProvider _comboProvider;

	public ActionComboBox(
		IComboboxDataProvider comboProvider,
		IServiceProvider serviceProvider,
		IColumnAlignmentResolver alignmentResolver)
		: base(serviceProvider, alignmentResolver)
	{
		_comboProvider = comboProvider;
	}

	protected override IList<KeyValuePair<short, string>> GetDataSource() =>
		_comboProvider.GetActions().ToList();

	protected override void AssignItemsProvider(RecipeComboBoxCell cell) =>
		cell.SetItemsProvider(ServiceProvider.GetRequiredService<ActionItemsProvider>());
}
