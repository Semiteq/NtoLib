using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class TableRowHeaderContextMenuService : IDisposable
{
	private readonly TableInputActions _actions;
	private readonly DataGridView _table;
	private bool _attached;
	private ToolStripMenuItem? _copyItem;
	private ToolStripMenuItem? _cutItem;
	private ToolStripMenuItem? _deleteItem;
	private ToolStripMenuItem? _newItem;
	private ToolStripMenuItem? _pasteItem;

	private ContextMenuStrip? _rowHeaderMenu;

	public TableRowHeaderContextMenuService(DataGridView table, TableInputActions actions)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_actions = actions ?? throw new ArgumentNullException(nameof(actions));
	}

	public void Dispose()
	{
		Detach();
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
				_rowHeaderMenu.Opening -= OnRowHeaderMenuOpening;
			}
			catch
			{
				// ignored
			}

			try
			{
				_rowHeaderMenu.Dispose();
			}
			catch
			{
				/* ignored */
			}

			_rowHeaderMenu = null;
			_copyItem = null;
			_cutItem = null;
			_pasteItem = null;
			_deleteItem = null;
			_newItem = null;
		}

		_attached = false;
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
		_rowHeaderMenu.Opening += OnRowHeaderMenuOpening;

		_copyItem = new ToolStripMenuItem("Копировать", null, async void (_, _) =>
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

		_cutItem = new ToolStripMenuItem("Вырезать", null, async void (_, _) =>
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

		_pasteItem = new ToolStripMenuItem("Вставить", null, async void (_, _) =>
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

		_deleteItem = new ToolStripMenuItem("Удалить", null, async void (_, _) =>
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

		_newItem = new ToolStripMenuItem("Создать новую", null, async void (_, _) =>
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
			_copyItem, _cutItem, _pasteItem, new ToolStripSeparator(), _deleteItem, new ToolStripSeparator(),
			_newItem
		});
	}

	private void OnRowHeaderMenuOpening(object? sender, CancelEventArgs e)
	{
		try
		{
			_copyItem!.Enabled = _actions.CanCopy();
			_cutItem!.Enabled = _actions.CanCut();
			_pasteItem!.Enabled = _actions.CanPaste();
			_deleteItem!.Enabled = _actions.CanDelete();
			_newItem!.Enabled = _actions.CanInsert();
		}
		catch
		{
			// ignored
		}
	}
}
