#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates DataGridViewTextBoxColumn instances for simple text display and editing.
/// </summary>
public class TextBoxColumnFactory : BaseColumnFactory
{
    /// <inheritdoc />
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new DataGridViewTextBoxColumn();
    }
}