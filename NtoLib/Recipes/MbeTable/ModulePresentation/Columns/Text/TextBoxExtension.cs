using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.Text;

public sealed class TextBoxExtension : FactoryColumnBase
{
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
        new DataGridViewTextBoxColumn();
}