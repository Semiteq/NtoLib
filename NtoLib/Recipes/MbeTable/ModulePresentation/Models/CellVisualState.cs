using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Models;

public readonly record struct CellVisualState(
	Font Font,
	Color ForeColor,
	Color BackColor,
	bool IsReadOnly,
	DataGridViewComboBoxDisplayStyle ComboDisplayStyle);
