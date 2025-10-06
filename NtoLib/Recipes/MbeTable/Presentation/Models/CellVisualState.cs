using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Models;

/// <summary>
/// Immutable representation of a cell's visual state including colors, fonts, and interaction flags.
/// </summary>
public readonly record struct CellVisualState(
    Font Font,
    Color ForeColor,
    Color BackColor,
    bool IsReadOnly,
    DataGridViewComboBoxDisplayStyle ComboDisplayStyle);