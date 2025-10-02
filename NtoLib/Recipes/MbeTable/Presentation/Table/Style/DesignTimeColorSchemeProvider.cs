#nullable enable

using System;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Style;

/// <summary>
/// Manages ColorScheme lifecycle for design-time property updates.
/// Thread-safe singleton that notifies subscribers of color changes.
/// </summary>
public sealed class DesignTimeColorSchemeProvider : IColorSchemeProvider
{
    private readonly object _lock = new object();
    private ColorScheme _current = ColorScheme.Default;

    /// <inheritdoc />
    public event Action<ColorScheme>? Changed;
    
    /// <inheritdoc />
    public ColorScheme Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    /// <summary>
    /// Updates the current ColorScheme and notifies all subscribers.
    /// Call this when design-time color properties change.
    /// </summary>
    /// <param name="newScheme">The new immutable ColorScheme instance.</param>
    public void Update(ColorScheme newScheme)
    {
        if (newScheme == null)
            throw new ArgumentNullException(nameof(newScheme));

        lock (_lock)
        {
            _current = newScheme;
        }

        Changed?.Invoke(newScheme);
    }
}