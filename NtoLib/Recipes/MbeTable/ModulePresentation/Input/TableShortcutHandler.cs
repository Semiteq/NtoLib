using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class TableShortcutHandler : IDisposable
{
	private readonly DataGridView _table;
	private readonly TableInputActions _actions;
	private bool _attached;

	public TableShortcutHandler(DataGridView table, TableInputActions actions)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_actions = actions ?? throw new ArgumentNullException(nameof(actions));
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.KeyDown += OnTableKeyDown;
		_attached = true;
	}

	public void Detach()
	{
		if (!_attached)
		{
			return;
		}

		try
		{
			_table.KeyDown -= OnTableKeyDown;
		}
		catch
		{
			/* ignored */
		}

		_attached = false;
	}

	public void Dispose()
	{
		Detach();
	}

	private async void OnTableKeyDown(object? sender, KeyEventArgs e)
	{
		try
		{
			await ProcessShortcutAsync(e).ConfigureAwait(true);
		}
		catch
		{
			/* ignored */
		}
	}

	private async Task ProcessShortcutAsync(KeyEventArgs e)
	{
		if (e.Control && e.KeyCode == Keys.C)
		{
			if (await _actions.TryCopyAsync().ConfigureAwait(true))
			{
				e.Handled = true;
			}

			return;
		}

		if (e.Control && e.KeyCode == Keys.X)
		{
			if (await _actions.TryCutAsync().ConfigureAwait(true))
			{
				e.Handled = true;
			}

			return;
		}

		if (e.Control && e.KeyCode == Keys.V)
		{
			if (await _actions.TryPasteAsync().ConfigureAwait(true))
			{
				e.Handled = true;
			}

			return;
		}

		if (e.Control && e.KeyCode == Keys.N)
		{
			if (await _actions.TryInsertAsync().ConfigureAwait(true))
			{
				e.Handled = true;
			}

			return;
		}

		if (!e.Control && e.KeyCode == Keys.Delete)
		{
			if (await _actions.TryDeleteAsync().ConfigureAwait(true))
			{
				e.Handled = true;
			}
		}
	}
}
