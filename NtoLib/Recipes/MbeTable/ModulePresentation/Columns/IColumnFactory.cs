using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

/// <summary>
/// Factory that builds and fully configures a <see cref="DataGridViewColumn"/>
/// according to YAML <see cref="ColumnDefinition"/> and active <see cref="ColorScheme"/>.
/// </summary>
public interface IColumnFactory
{
    /// <summary>
    /// Creates a <see cref="DataGridViewColumn"/> instance and applies default styling.
    /// </summary>
    DataGridViewColumn CreateColumn(ColumnDefinition definition, ColorScheme scheme);
}