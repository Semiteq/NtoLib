using System;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Presentation.Models;
using NtoLib.Recipes.MbeTable.Presentation.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Style;

/// <summary>
/// Represents an immutable set of colors and fonts for styling the recipe table UI.
/// Provides a centralized lookup for mapping state combinations to visual styles.
/// </summary>
public record ColorScheme
{
    public static ColorScheme Default { get; } = new();

    // Background colors
    public Color ControlBackgroundColor { get; init; }
    public Color TableBackgroundColor { get; init; }
    public Color StatusBgColor { get; init; }

    // Fonts
    public Font HeaderFont { get; init; }
    public Font LineFont { get; init; }
    public Font SelectedLineFont { get; init; }
    public Font PassedLineFont { get; init; }
    public Font BlockedFont { get; init; }

    // Header colors
    public Color HeaderBgColor { get; init; }
    public Color HeaderTextColor { get; init; }

    // Normal line colors
    public Color LineBgColor { get; init; }
    public Color LineTextColor { get; init; }

    // Selected (current) line colors
    public Color SelectedLineBgColor { get; init; }
    public Color SelectedLineTextColor { get; init; }

    // Passed line colors
    public Color PassedLineBgColor { get; init; }
    public Color PassedLineTextColor { get; init; }

    // Blocked/disabled colors
    public Color BlockedBgColor { get; init; }
    public Color BlockedTextColor { get; init; }

    // Button colors
    public Color ButtonsColor { get; init; }
    public Color BlockedButtonsColor { get; init; }

    // Sizing
    public int LineHeight { get; init; }

    // Selection outline
    public Color SelectedOutlineColor { get; init; }
    public int SelectedOutlineThickness { get; init; }

    public ColorScheme()
    {
        // Background colors
        ControlBackgroundColor = SystemColors.Control;
        TableBackgroundColor = SystemColors.Window;
        StatusBgColor = SystemColors.ControlLight;

        // Fonts
        HeaderFont = new Font("Arial", 14f, FontStyle.Bold);
        LineFont = new Font("Arial", 12f);
        SelectedLineFont = new Font("Arial", 12f, FontStyle.Bold);
        PassedLineFont = new Font("Arial", 12f);
        BlockedFont = new Font("Arial", 12f);

        // Header colors
        HeaderBgColor = SystemColors.ControlLight;
        HeaderTextColor = Color.Black;

        // Normal line colors
        LineBgColor = SystemColors.Window;
        LineTextColor = Color.Black;

        // Selected (current) line colors
        SelectedLineBgColor = Color.DeepSkyBlue;
        SelectedLineTextColor = Color.White;

        // Passed line colors
        PassedLineBgColor = Color.Khaki;
        PassedLineTextColor = Color.Black;

        // Blocked/disabled colors
        BlockedBgColor = Color.LightGray;
        BlockedTextColor = Color.Black;

        // Button colors
        ButtonsColor = SystemColors.Control;
        BlockedButtonsColor = Color.LightGray;

        // Sizing
        LineHeight = 25;

        // Selection outline
        SelectedOutlineColor = Color.DeepSkyBlue;
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