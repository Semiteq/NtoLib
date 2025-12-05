using NtoLib.MbeTable.ModuleApplication.ViewModels;

namespace NtoLib.MbeTable.ModulePresentation.DataAccess;

/// <summary>
/// Minimal adapter that provides read-only access to virtualised row data for Presentation layer.
/// </summary>
public interface ICellDataContext
{
	/// <summary>
	/// Returns <see cref="StepViewModel"/> for the given row or <c>null</c> when index is out of range.
	/// </summary>
	StepViewModel? GetStepViewModel(int rowIndex);

	/// <summary>
	/// Total row count (cheap).
	/// </summary>
	int RowCount { get; }
}
