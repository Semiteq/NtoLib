using System;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Style;

/// <summary>
/// Represents an immutable set of colors and fonts for styling the recipe table UI.
/// Provides a centralized lookup for mapping state combinations to visual styles.
/// </summary>
public record ColorScheme
{
    public static ColorScheme Default { get; } = new();

    public Color ControlBackgroundColor { get; init; }
    public Color TableBackgroundColor { get; init; }

    public Font HeaderFont { get; init; }
    public Font LineFont { get; init; }
    public Font SelectedLineFont { get; init; }
    public Font PassedLineFont { get; init; }
    public Font BlockedFont { get; init; }

    public Color HeaderTextColor { get; init; }
    public Color LineTextColor { get; init; }
    public Color SelectedLineTextColor { get; init; }
    public Color PassedLineTextColor { get; init; }
    public Color BlockedTextColor { get; init; }

    public Color HeaderBgColor { get; init; }
    public Color LineBgColor { get; init; }
    public Color SelectedLineBgColor { get; init; }
    public Color PassedLineBgColor { get; init; }
    public Color BlockedBgColor { get; init; }

    public Color ButtonsColor { get; init; }
    public Color BlockedButtonsColor { get; init; }

    public int LineHeight { get; init; }

    public Color StatusBgColor { get; init; }

    public Color SelectedOutlineColor { get; init; }
    public int SelectedOutlineThickness { get; init; }

    public ColorScheme()
    {
        ControlBackgroundColor = SystemColors.Control;
        TableBackgroundColor = SystemColors.Window;

        HeaderFont = new Font("Arial", 14f, FontStyle.Bold);
        LineFont = new Font("Arial", 12f);
        SelectedLineFont = new Font("Arial", 12f, FontStyle.Bold);
        PassedLineFont = new Font("Arial", 12f);
        BlockedFont = new Font("Arial", 12f);

        HeaderTextColor = SystemColors.ControlText;
        LineTextColor = SystemColors.WindowText;
        SelectedLineTextColor = Color.White;
        PassedLineTextColor = Color.DarkGray;
        BlockedTextColor = Color.DarkGray;

        HeaderBgColor = SystemColors.ControlLight;
        LineBgColor = SystemColors.Window;
        SelectedLineBgColor = Color.FromArgb(0, 120, 215);
        PassedLineBgColor = Color.FromArgb(240, 240, 240);
        BlockedBgColor = Color.FromArgb(170, 170, 170);

        ButtonsColor = SystemColors.Control;
        BlockedButtonsColor = Color.FromArgb(170, 170, 170);

        LineHeight = 25;

        StatusBgColor = SystemColors.ControlLight;

        SelectedOutlineColor = Color.FromArgb(0, 120, 215);
        SelectedOutlineThickness = 1;
    }

    /// <summary>
    /// Resolves the visual state for a cell based on row execution state and cell data state.
    /// Priority: Current and Passed row states override all data state styling.
    /// </summary>
    /// <param name="rowState">The execution state of the row.</param>
    /// <param name="dataState">The data availability state of the cell.</param>
    /// <returns>The resolved visual state.</returns>
    public CellVisualState GetStyleForState(RowExecutionState rowState, CellDataState dataState)
    {
        return (rowState, dataState) switch
        {
            (RowExecutionState.Current, _) => new CellVisualState(
                Font: SelectedLineFont,
                ForeColor: SelectedLineTextColor,
                BackColor: SelectedLineBgColor,
                IsReadOnly: true,
                ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.Nothing),

            (RowExecutionState.Passed, _) => new CellVisualState(
                Font: PassedLineFont,
                ForeColor: PassedLineTextColor,
                BackColor: PassedLineBgColor,
                IsReadOnly: true,
                ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.Nothing),

            (RowExecutionState.Upcoming, CellDataState.Normal) => new CellVisualState(
                Font: LineFont,
                ForeColor: LineTextColor,
                BackColor: LineBgColor,
                IsReadOnly: false,
                ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.DropDownButton),

            // ReadOnly: gray background, shows value, not editable
            (RowExecutionState.Upcoming, CellDataState.ReadOnly) => new CellVisualState(
                Font: BlockedFont,
                ForeColor: BlockedTextColor,
                BackColor: BlockedBgColor,
                IsReadOnly: true,
                ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.Nothing),

            // Disabled: gray background, no value (property missing)
            (RowExecutionState.Upcoming, CellDataState.Disabled) => new CellVisualState(
                Font: BlockedFont,
                ForeColor: BlockedTextColor,
                BackColor: BlockedBgColor,
                IsReadOnly: true,
                ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.Nothing),

            _ => throw new ArgumentOutOfRangeException(nameof(rowState))
        };
    }
}