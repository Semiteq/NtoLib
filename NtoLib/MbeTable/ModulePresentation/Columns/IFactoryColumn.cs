using System.Windows.Forms;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModulePresentation.Style;

namespace NtoLib.MbeTable.ModulePresentation.Columns;

/// <summary>
/// Factory that builds and fully configures a <see cref="DataGridViewColumn"/>
/// according to YAML <see cref="ColumnDefinition"/> and active <see cref="ColorScheme"/>.
/// </summary>
public interface IFactoryColumn
{
	/// <summary>
	/// Creates a <see cref="DataGridViewColumn"/> instance and applies default styling.
	/// </summary>
	DataGridViewColumn CreateColumn(ColumnDefinition definition, ColorScheme scheme);
}
