using System.Drawing;

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

    // Selection outline for the current cell
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
        PassedLineBgColor = Color.Transparent;
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
        RowSelectionBgColor = ColorStyleHelpers.Blend(LineBgColor, SelectedLineBgColor, 0.3f);
        RowSelectionTextColor = LineTextColor;

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

}