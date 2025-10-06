

using System;

namespace NtoLib.Recipes.MbeTable.Presentation.Style;

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

    /// <summary>
    /// Applies a mutation function to the current scheme and publishes the result.
    /// Used by design-time property setters to update individual color fields.
    /// </summary>
    /// <param name="mutate">Function that returns a modified copy of the current scheme.</param>
    public void Mutate(Func<ColorScheme, ColorScheme> mutate)
    {
        if (mutate == null)
            throw new ArgumentNullException(nameof(mutate));

        ColorScheme updated;
        lock (_lock)
        {
            updated = mutate(_current);
            _current = updated;
        }

        Changed?.Invoke(updated);
    }
}