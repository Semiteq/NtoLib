using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.ComboBox;

public sealed class ActionComboBox : FactoryColumnComboBoxBase
{
	private readonly ComboboxDataProvider _comboProvider;

	public ActionComboBox(
		ComboboxDataProvider comboProvider,
		IServiceProvider serviceProvider)
		: base(serviceProvider)
	{
		_comboProvider = comboProvider;
	}

	protected override IList<KeyValuePair<short, string>> GetDataSource()
	{
		return _comboProvider.GetActions().ToList();
	}

	protected override void AssignItemsProvider(RecipeComboBoxCell cell)
	{
		cell.SetItemsProvider(ServiceProvider.GetRequiredService<ActionItemsProvider>());
	}
}
