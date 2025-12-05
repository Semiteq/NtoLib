namespace NtoLib.MbeTable.ModulePresentation.Rendering;

/// <summary>
/// Stateless service that draws specific cell types (text, combo, etc.).
/// </summary>
public interface ICellRenderer
{
	/// <summary>
	/// Renders cell according to given context.
	/// </summary>
	void Render(in CellRenderContext context);
}
