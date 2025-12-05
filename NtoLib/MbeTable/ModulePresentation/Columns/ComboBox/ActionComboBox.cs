using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.MbeTable.ModuleCore.Services;
using NtoLib.MbeTable.ModulePresentation.Cells;
using NtoLib.MbeTable.ModulePresentation.DataAccess;
using NtoLib.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.MbeTable.ModulePresentation.Columns.ComboBox;

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
