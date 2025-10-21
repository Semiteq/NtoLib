

using System;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Style;

/// <summary>
/// Provides reactive access to the current <see cref="ColorScheme"/>.
/// Notifies subscribers when design-time color properties change.
/// </summary>
public interface IColorSchemeProvider
{
    /// <summary>
    /// Gets the currently active ColorScheme.
    /// </summary>
    ColorScheme Current { get; }

    /// <summary>
    /// Occurs when the ColorScheme is updated from design-time properties.
    /// Subscribers should re-apply styles based on the new scheme.
    /// </summary>
    event Action<ColorScheme>? Changed;
}