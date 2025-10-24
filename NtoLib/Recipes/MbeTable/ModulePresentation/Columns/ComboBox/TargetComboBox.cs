using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.ComboBox;

public sealed class TargetComboBox : FactoryColumnComboBoxBase
{
    public TargetComboBox(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    protected override IList<KeyValuePair<short, string>> GetDataSource() =>
        new List<KeyValuePair<short, string>>();

    protected override void AssignItemsProvider(RecipeComboBoxCell cell) =>
        cell.SetItemsProvider(ServiceProvider.GetRequiredService<TargetItemsProvider>());
}