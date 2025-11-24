using System;
using System.Drawing;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModulePresentation.Models;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Style;

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

    // Selected (current execution) line colors
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

    // Selection outline for current cell
    public Color SelectedOutlineColor { get; init; }
    public int SelectedOutlineThickness { get; init; }

    // Status colorscheme
    public Color StatusInfoColor { get; init; }
    public Color StatusSuccessColor { get; init; }
    public Color StatusWarningColor { get; init; }
    public Color StatusErrorColor { get; init; }
    public Color StatusBgColor { get; init; }

    // User row selection (DataGridView row selection, not execution "Current" state)
    public Color RowSelectionBgColor { get; init; }
    public Color RowSelectionTextColor { get; init; }

    // Loop nesting background colors (depth 1..3)
    public Color LoopLevel1BgColor { get; init; }
    public Color LoopLevel2BgColor { get; init; }
    public Color LoopLevel3BgColor { get; init; }

    // Loop tint weights
    public float LoopLevel1TintWeight { get; init; }
    public float LoopLevel2TintWeight { get; init; }
    public float LoopLevel3TintWeight { get; init; }

    // Execution tint weights
    public float CurrentExecutionTintWeight { get; init; }
    public float PassedExecutionTintWeight { get; init; }

    public ColorScheme()
    {
        // Background colors
        ControlBackgroundColor = SystemColors.Control;
        TableBackgroundColor = SystemColors.Window;

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

        // Selected (current execution) line colors
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

        // Status colorscheme
        StatusInfoColor = ControlBackgroundColor;
        StatusSuccessColor = Color.LightGreen;
        StatusWarningColor = Color.Gold;
        StatusErrorColor = Color.LightCoral;
        StatusBgColor = SystemColors.ControlLight;

        // User row selection (tinted variant of normal line color)
        RowSelectionBgColor = CreateTint(LineBgColor, SelectedLineBgColor, 0.3f);
        RowSelectionTextColor = LineTextColor;

        // Loop nesting colors (chosen distinctive purples)
        // Depth 1 - Royal Purple, Depth 2 - Twitch Purple, Depth 3 - Bright Purple
        LoopLevel1BgColor = Color.FromArgb(0x4B, 0x00, 0x82); // Royal Purple
        LoopLevel2BgColor = Color.FromArgb(0x64, 0x41, 0xA5); // Twitch Purple (approx)
        LoopLevel3BgColor = Color.FromArgb(0xBF, 0x40, 0xBF); // Bright Purple

        // Tint weights
        LoopLevel1TintWeight = 0.25f;
        LoopLevel2TintWeight = 0.45f;
        LoopLevel3TintWeight = 0.65f;

        CurrentExecutionTintWeight = 0.55f;
        PassedExecutionTintWeight = 0.40f;
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

    public Color ApplyLoopTint(Color baseColor, int depth, bool isRestricted)
    {
        if (depth <= 0) return baseColor;
        float w = depth switch
        {
            1 => LoopLevel1TintWeight,
            2 => LoopLevel2TintWeight,
            3 => LoopLevel3TintWeight,
            _ => LoopLevel3TintWeight
        };
        if (isRestricted) w *= 0.6f; // reduce impact for disabled/readonly
        var accent = depth switch
        {
            1 => LoopLevel1BgColor,
            2 => LoopLevel2BgColor,
            3 => LoopLevel3BgColor,
            _ => LoopLevel3BgColor
        };
        return Blend(baseColor, accent, w);
    }

    public Color ApplyExecutionTint(Color baseColor, RowExecutionState state, bool isRestricted)
    {
        float w = state switch
        {
            RowExecutionState.Current => CurrentExecutionTintWeight,
            RowExecutionState.Passed => PassedExecutionTintWeight,
            _ => 0f
        };
        if (w <= 0f) return baseColor;
        if (isRestricted) w *= 0.6f;
        var accent = state switch
        {
            RowExecutionState.Current => SelectedLineBgColor,
            RowExecutionState.Passed => PassedLineBgColor,
            _ => baseColor
        };
        return Blend(baseColor, accent, w);
    }

    public Color Blend(Color baseColor, Color accentColor, float weight)
    {
        if (weight <= 0f) return baseColor;
        if (weight > 1f) weight = 1f;
        float inv = 1f - weight;
        int r = (int)(baseColor.R * inv + accentColor.R * weight);
        int g = (int)(baseColor.G * inv + accentColor.G * weight);
        int b = (int)(baseColor.B * inv + accentColor.B * weight);
        return Color.FromArgb(baseColor.A, ClampToByte(r), ClampToByte(g), ClampToByte(b));
    }

    public Color EnsureContrast(Color backColor, Color foreColor)
    {
        // Simple luminance-based adjustment; keep existing if sufficiently distinct.
        int lumBack = (int)(0.299 * backColor.R + 0.587 * backColor.G + 0.114 * backColor.B);
        int lumFore = (int)(0.299 * foreColor.R + 0.587 * foreColor.G + 0.114 * foreColor.B);
        if (Math.Abs(lumBack - lumFore) > 60) return foreColor;
        // Choose black or white based on background luminance midpoint.
        return lumBack < 128 ? Color.White : Color.Black;

    }
    private static Color CreateTint(Color baseColor, Color accentColor, float accentWeight)
    {
        if (accentWeight < 0f) accentWeight = 0f;
        if (accentWeight > 1f) accentWeight = 1f;
        float baseWeight = 1f - accentWeight;
        int r = (int)(baseColor.R * baseWeight + accentColor.R * accentWeight);
        int g = (int)(baseColor.G * baseWeight + accentColor.G * accentWeight);
        int b = (int)(baseColor.B * baseWeight + accentColor.B * accentWeight);
        return Color.FromArgb(baseColor.A, ClampToByte(r), ClampToByte(g), ClampToByte(b));
    }

    private static int ClampToByte(int value)
    {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return value;
    }
}