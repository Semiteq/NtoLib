using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Table.UI.TableUpdate;

public struct ColorScheme
{
    // User can only change UI parameters when changing "To Design" => "To Runtime" state.
    // No defaults are applied dew to UI stores the default values.
    
    // Table background colors
    public Color ControlBackgroundColor;
    public Color TableBackgroundColor;
        
    // Table fonts
    public Font HeaderFont;
    public Font LineFont;
    public Font SelectedLineFont;
    public Font PassedLineFont;

    // Line text colors
    public Color HeaderTextColor;
    public Color LineTextColor;
    public Color SelectedLineTextColor;
    public Color PassedLineTextColor;
        
    // Line background colors
    public Color HeaderBgColor;
    public Color LineBgColor;
    public Color SelectedLineBgColor;
    public Color PassedLineBgColor;
        
    // Buttons
    public Color ButtonsColor;
    public int ButtonsSize;
}