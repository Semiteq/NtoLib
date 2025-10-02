#nullable enable

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

/// <summary>
/// Text cell that ignores selection background and draws with the provided cell style.
/// Used for read-only informational fields like step start time.
/// </summary>
public sealed class ReadonlyLabelCell : NonSelectableCellBase
{
}