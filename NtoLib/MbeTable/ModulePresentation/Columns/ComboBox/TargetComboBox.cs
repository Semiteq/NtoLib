using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.MbeTable.ModulePresentation.Cells;
using NtoLib.MbeTable.ModulePresentation.DataAccess;
using NtoLib.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.MbeTable.ModulePresentation.Columns.ComboBox;

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
