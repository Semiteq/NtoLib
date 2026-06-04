namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Read-only view over the defaulted-cell mark store, consumed by the rendering chain.
/// </summary>
public interface IDefaultedCellsReader
{
	bool IsMarked(int row, int col);
}
