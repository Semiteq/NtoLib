using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Style;

/// <summary>
/// Represents an immutable set of colors and fonts for styling the recipe table UI.
/// </summary>
public record ColorScheme
{
    // --- Static ---

    /// <summary>
    /// Provides a single, shared instance with default theme values.
    /// This is the Single Source of Truth for default styles.
    /// </summary>
    public static ColorScheme Default { get; } = new();

    // --- Properties ---

    // Control/table background
    public Color ControlBackgroundColor { get; init; }
    public Color TableBackgroundColor { get; init; }

    // Fonts
    public Font HeaderFont { get; init; }
    public Font LineFont { get; init; }
    public Font SelectedLineFont { get; init; }
    public Font PassedLineFont { get; init; }
    public Font BlockedFont { get; init; }

    // Line text colors
    public Color HeaderTextColor { get; init; }
    public Color LineTextColor { get; init; }
    public Color SelectedLineTextColor { get; init; }
    public Color PassedLineTextColor { get; init; }
    public Color BlockedTextColor { get; init; }

    // Line background colors
    public Color HeaderBgColor { get; init; }
    public Color LineBgColor { get; init; }
    public Color SelectedLineBgColor { get; init; }
    public Color PassedLineBgColor { get; init; }
    public Color BlockedBgColor { get; init; }

    // Buttons
    public Color ButtonsColor { get; init; }
    public Color BlockedButtonsColor { get; init; }

    // Sizes
    public int LineHeight { get; init; }

    // Status area background
    public Color StatusBgColor { get; init; }

    // Focus (keyboard cursor) outline for the current cell
    public Color SelectedOutlineColor { get; init; }
    public int SelectedOutlineThickness { get; init; }

    /// <summary>
    /// Creates a ColorScheme with a set of default values.
    /// Used to initialize the static Default instance.
    /// </summary>
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
        BlockedBgColor = SystemColors.ControlLight;

        ButtonsColor = SystemColors.Control;
        BlockedButtonsColor = Color.FromArgb(170, 170, 170);

        LineHeight = 40;

        StatusBgColor = SystemColors.ControlLight;

        // Focus outline defaults
        SelectedOutlineColor = Color.FromArgb(0, 120, 215);
        SelectedOutlineThickness = 1;
    }
}