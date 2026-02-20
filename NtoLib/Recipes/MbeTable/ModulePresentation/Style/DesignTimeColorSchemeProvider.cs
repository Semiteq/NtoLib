using System;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Style;

public sealed class DesignTimeColorSchemeProvider
{
	private readonly object _lock = new();
	private ColorScheme _current = ColorScheme.Default;

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

	public event Action<ColorScheme>? Changed;

	public void Mutate(Func<ColorScheme, ColorScheme> mutate)
	{
		if (mutate == null)
		{
			throw new ArgumentNullException(nameof(mutate));
		}

		ColorScheme updated;
		lock (_lock)
		{
			updated = mutate(_current);
			_current = updated;
		}

		Changed?.Invoke(updated);
	}
}
