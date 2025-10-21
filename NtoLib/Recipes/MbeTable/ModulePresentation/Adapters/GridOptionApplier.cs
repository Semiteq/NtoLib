using System.Reflection;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;

/// <summary>Sets VirtualMode / selection / double-buffer flags once.</summary>
internal static class GridOptionsApplier
{
    public static void Apply(DataGridView grid)
    {
        grid.VirtualMode = true;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.EditMode = DataGridViewEditMode.EditOnEnter;
        grid.EnableHeadersVisualStyles = false;

        // Enable double buffering by reflection (WinForms hack)
        typeof(DataGridView).InvokeMember(
            "DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null,
            grid,
            new object[] { true });
    }
}