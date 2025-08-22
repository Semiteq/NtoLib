using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Style;

public record ColorScheme(
    // Table background colors
    Color ControlBackgroundColor,
    Color TableBackgroundColor,

    // Table fonts
    Font HeaderFont,
    Font LineFont,
    Font SelectedLineFont,
    Font PassedLineFont,
    Font BlockedFont,

    // Line text colors
    Color HeaderTextColor,
    Color LineTextColor,
    Color SelectedLineTextColor,
    Color PassedLineTextColor,
    Color BlockedTextColor,

    // Line background colors
    Color HeaderBgColor,
    Color LineBgColor,
    Color SelectedLineBgColor,
    Color PassedLineBgColor,
    Color BlockedBgColor,

    // Buttons
    Color ButtonsColor,
    Color BlockedButtonsColor,

    // Sizes
    int LineHeight
);
