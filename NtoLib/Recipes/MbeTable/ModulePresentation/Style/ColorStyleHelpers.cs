using System;
using System.Drawing;

using NtoLib.Recipes.MbeTable.ModulePresentation.Models;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Style;

internal static class ColorStyleHelpers
{
    public static Color ApplyLoopTint(Color baseColor, int depth, bool isRestricted, ColorScheme scheme)
    {
        if (depth <= 0) 
            return baseColor;
        
        var weight = depth switch
        {
            1 => scheme.LoopLevel1TintWeight,
            2 => scheme.LoopLevel2TintWeight,
            3 => scheme.LoopLevel3TintWeight,
            _ => scheme.LoopLevel3TintWeight
        };
        
        if (isRestricted) 
            weight *= 0.6f;
        
        var accent = depth switch
        {
            1 => scheme.LoopLevel1BgColor,
            2 => scheme.LoopLevel2BgColor,
            3 => scheme.LoopLevel3BgColor,
            _ => scheme.LoopLevel3BgColor
        };
        
        return Blend(baseColor, accent, weight);
    }

    public static Color ApplyExecutionTint(Color baseColor, RowExecutionState state, bool isRestricted, ColorScheme scheme)
    {
        var weight = state switch
        {
            RowExecutionState.Current => scheme.CurrentExecutionTintWeight,
            RowExecutionState.Passed => scheme.PassedExecutionTintWeight,
            _ => 0f
        };
        
        if (weight <= 0f) 
            return baseColor;
        
        if (isRestricted) 
            weight *= 0.6f;
        
        var accent = state switch
        {
            RowExecutionState.Current => scheme.SelectedLineBgColor,
            RowExecutionState.Passed => scheme.PassedLineBgColor,
            _ => baseColor
        };
        
        return Blend(baseColor, accent, weight);
    }

    public static Color Blend(Color baseColor, Color accentColor, float weight)
    {
        if (accentColor == Color.Transparent || accentColor == Color.Empty) 
            return baseColor;
        
        switch (weight)
        {
            case <= 0f:
                return baseColor;
            case > 1f:
                weight = 1f;
                break;
        }

        var inv = 1f - weight;
        
        var r = (int)(baseColor.R * inv + accentColor.R * weight);
        var g = (int)(baseColor.G * inv + accentColor.G * weight);
        var b = (int)(baseColor.B * inv + accentColor.B * weight);
        
        return Color.FromArgb(baseColor.A, ClampToByte(r), ClampToByte(g), ClampToByte(b));
    }

    public static Color EnsureContrast(Color backColor, Color foreColor)
    {
        var lumBack = (int)(0.299 * backColor.R + 0.587 * backColor.G + 0.114 * backColor.B);
        var lumFore = (int)(0.299 * foreColor.R + 0.587 * foreColor.G + 0.114 * foreColor.B);
        
        if (Math.Abs(lumBack - lumFore) > 60) 
            return foreColor;
        
        return lumBack < 128 ? Color.White : Color.Black;
    }

    private static int ClampToByte(int value)
    {
        return value switch
        {
            < 0 => 0,
            > 255 => 255,
            _ => value
        };
    }
}
