namespace NtoLib.MbeTable.ModulePresentation.Models;

/// <summary>
/// Represents the data availability and editability state of a cell.
/// Lower priority than RowExecutionState for visual styling.
/// </summary>
public enum CellDataState
{
	/// <summary>
	/// Cell contains valid data and is editable by the user.
	/// Visual: White background, enabled editing controls. (color may vary by user)
	/// </summary>
	Normal,

	/// <summary>
	/// Cell contains valid data but is explicitly marked as read-only in column configuration.
	/// Example: Step start time (calculated field), comment field marked readonly.
	/// Visual: Gray background, editing disabled. (color may vary by user)
	/// </summary>
	ReadOnly,

	/// <summary>
	/// Cell property does not exist for the current Action (property is null/not applicable).
	/// Example: Channel selector for Wait_time action (channel not used).
	/// Visual: Gray background, editing disabled, ComboBox dropdown hidden. (color may vary by user)
	/// </summary>
	Disabled
}
