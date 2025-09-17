#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

public sealed class ActionComboBoxCell : BaseRecipeComboBoxCell
{
    protected override List<KeyValuePair<int, string>>? ProvideRowItems(StepViewModel vm, ColumnIdentifier key) => null;
}