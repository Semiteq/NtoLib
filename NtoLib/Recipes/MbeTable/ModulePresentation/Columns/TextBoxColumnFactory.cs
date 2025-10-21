using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

/// <summary>
/// Plain text column factory.
/// </summary>
public sealed class TextBoxColumnFactory : BaseColumnFactory
{
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
        new DataGridViewTextBoxColumn
        {
            DataPropertyName = definition.Key.Value
        };
}