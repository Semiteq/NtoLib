using System;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class TableRowHeaderContextMenuService : IDisposable
{
	private readonly DataGridView _table;
	private readonly TableInputActions _actions;

	private ContextMenuStrip? _rowHeaderMenu;
	private bool _attached;

	public TableRowHeaderContextMenuService(DataGridView table, TableInputActions actions)
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

		_table.RowHeaderMouseClick += OnRowHeaderMouseClick;
		InitializeRowHeaderContextMenu();
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
			_table.RowHeaderMouseClick -= OnRowHeaderMouseClick;
		}
		catch
		{
			/* ignored */
		}

		if (_rowHeaderMenu != null)
		{
			try
			{
				_rowHeaderMenu.Dispose();
			}
			catch
			{
				/* ignored */
			}

			_rowHeaderMenu = null;
		}

		_attached = false;
	}

	public void Dispose()
	{
		Detach();
	}

	private void OnRowHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.RowIndex < 0)
		{
			return;
		}

		if (e.Button == MouseButtons.Right)
		{
			var currentPosition = new Point(Cursor.Position.X, Cursor.Position.Y);
			_rowHeaderMenu?.Show(currentPosition);
		}
	}

	private void InitializeRowHeaderContextMenu()
	{
		_rowHeaderMenu = new ContextMenuStrip();

		var copyItem = new ToolStripMenuItem("Copy rows", null, async (_, _) =>
		{
			try
			{
				await _actions.TryCopyAsync().ConfigureAwait(true);
			}
			catch
			{
				/* ignored */
			}
		});

		var cutItem = new ToolStripMenuItem("Cut rows", null, async (_, _) =>
		{
			try
			{
				await _actions.TryCutAsync().ConfigureAwait(true);
			}
			catch
			{
				/* ignored */
			}
		});

		var pasteItem = new ToolStripMenuItem("Paste rows", null, async (_, _) =>
		{
			try
			{
				await _actions.TryPasteAsync().ConfigureAwait(true);
			}
			catch
			{
				/* ignored */
			}
		});

		var deleteItem = new ToolStripMenuItem("Delete rows", null, async (_, _) =>
		{
			try
			{
				await _actions.TryDeleteAsync().ConfigureAwait(true);
			}
			catch
			{
				/* ignored */
			}
		});

		var newItem = new ToolStripMenuItem("New row", null, async (_, _) =>
		{
			try
			{
				await _actions.TryInsertAsync().ConfigureAwait(true);
			}
			catch
			{
				/* ignored */
			}
		});

		_rowHeaderMenu.Items.AddRange(new ToolStripItem[]
		{
			copyItem,
			cutItem,
			pasteItem,
			new ToolStripSeparator(),
			deleteItem,
			new ToolStripSeparator(),
			newItem
		});
	}
}
