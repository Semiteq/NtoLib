#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Context;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates DataGridViewTextBoxColumn instances for simple text display and editing.
/// </summary>
public sealed class TextBoxColumnFactory : BaseColumnFactory
{
    public TextBoxColumnFactory(IComboBoxContext comboBoxContext)
        : base(comboBoxContext)
    {
    }

    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition columnDefinition)
    {
        return new DataGridViewTextBoxColumn();
    }
}