namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Describes the scope of a defaulted-cell mark change. A non-null <see cref="Row"/>
/// requests a single-row repaint; <c>null</c> requests a full-grid repaint.
/// </summary>
public sealed record MarksChange(int? Row);
