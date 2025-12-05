using System.Reflection;
using System.Windows.Forms;

namespace NtoLib.MbeTable.ModulePresentation.Adapters;

/// <summary>
/// Applies common DataGridView options once during table initialization.
/// </summary>
internal static class GridOptionsApplier
{
	public static void Apply(DataGridView grid)
	{
		grid.VirtualMode = true;
		grid.AutoGenerateColumns = false;
		grid.AllowUserToAddRows = false;
		grid.AllowUserToDeleteRows = false;

		grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		grid.MultiSelect = true;

		grid.EditMode = DataGridViewEditMode.EditOnEnter;
		grid.EnableHeadersVisualStyles = false;

		EnableDoubleBuffering(grid);
	}

	private static void EnableDoubleBuffering(DataGridView grid)
	{
		typeof(DataGridView).InvokeMember(
			"DoubleBuffered",
			BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
			null,
			grid,
			new object[] { true });
	}
}
