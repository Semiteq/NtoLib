using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;
using NtoLib.Recipes.MbeTable.Utilities;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;

/// <summary>
/// Thin orchestrator that wires DataGridView events to
/// <see cref="CellFormattingEngine"/> and <see cref="EditGatingCoordinator"/>,
/// handles UI-thread marshalling, and manages the attach/detach lifecycle.
/// </summary>
public sealed class TableRenderCoordinator : IDisposable
{
	private readonly DesignTimeColorSchemeProvider _colorSchemeProvider;
	private readonly EditGatingCoordinator _editGating;

	private readonly CellFormattingEngine _formattingEngine;
	private readonly ILogger<TableRenderCoordinator> _logger;
	private readonly ThreadSafeRowExecutionStateProvider _rowExecutionStateProvider;
	private readonly DataGridView _table;
	private bool _disposed;

	private bool _initialized;

	public TableRenderCoordinator(
		DataGridView table,
		ThreadSafeRowExecutionStateProvider rowExecutionStateProvider,
		CellStateResolver cellStateResolver,
		RecipeViewModel recipeViewModel,
		IReadOnlyList<ColumnDefinition> columns,
		ILogger<TableRenderCoordinator> logger,
		DesignTimeColorSchemeProvider colorSchemeProvider)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_rowExecutionStateProvider = rowExecutionStateProvider ??
									 throw new ArgumentNullException(nameof(rowExecutionStateProvider));
		_colorSchemeProvider = colorSchemeProvider ?? throw new ArgumentNullException(nameof(colorSchemeProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		_formattingEngine = new CellFormattingEngine(
			table, cellStateResolver, recipeViewModel,
			rowExecutionStateProvider, colorSchemeProvider, columns);

		_editGating = new EditGatingCoordinator(table, recipeViewModel, logger);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (_initialized)
		{
			DetachEventHandlers();
		}
	}

	public void Initialize()
	{
		if (_initialized || _disposed)
		{
			return;
		}

		AttachEventHandlers();
		_initialized = true;
		ForceInitialFormatting();
	}

	private void AttachEventHandlers()
	{
		_table.CellFormatting += OnCellFormatting;
		_table.CellBeginEdit += _editGating.HandleCellBeginEdit;
		_table.CurrentCellDirtyStateChanged += _editGating.HandleCurrentCellDirtyStateChanged;
		_table.CellPainting += OnCellPaintingPreFormat;
		_rowExecutionStateProvider.CurrentLineChanged += OnCurrentLineChanged;
		_colorSchemeProvider.Changed += OnColorSchemeChanged;
	}

	private void DetachEventHandlers()
	{
		SafeDisposal.RunAll(
			() => _table.CellFormatting -= OnCellFormatting,
			() => _table.CellBeginEdit -= _editGating.HandleCellBeginEdit,
			() => _table.CurrentCellDirtyStateChanged -= _editGating.HandleCurrentCellDirtyStateChanged,
			() => _table.CellPainting -= OnCellPaintingPreFormat,
			() => _rowExecutionStateProvider.CurrentLineChanged -= OnCurrentLineChanged,
			() => _colorSchemeProvider.Changed -= OnColorSchemeChanged);
	}

	private void ForceInitialFormatting()
	{
		if (!_table.IsHandleCreated || _table.IsDisposed)
		{
			return;
		}

		InvokeOnUiThread(FormatAllCells);
	}

	private void OnCellPaintingPreFormat(object? sender, DataGridViewCellPaintingEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
		{
			return;
		}

		ApplyCellFormattingSafe(e.RowIndex, e.ColumnIndex);
	}

	private void OnColorSchemeChanged(ColorScheme colorScheme)
	{
		InvokeOnUiThread(RefreshTableAppearance);
	}

	private void RefreshTableAppearance()
	{
		try
		{
			FormatAllCells();
			_table.Invalidate();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to refresh table appearance after color scheme change");
		}
	}

	private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
		{
			return;
		}

		ApplyCellFormattingSafe(e.RowIndex, e.ColumnIndex, e.CellStyle);
	}

	private void OnCurrentLineChanged(int oldIndex, int newIndex)
	{
		InvokeOnUiThread(() => RefreshExecutionStateRows(oldIndex, newIndex));
	}

	private void RefreshExecutionStateRows(int oldIndex, int newIndex)
	{
		try
		{
			RefreshRowIfValid(oldIndex);
			RefreshRowIfValid(newIndex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to refresh execution state rows (old: {OldIndex}, new: {NewIndex})", oldIndex,
				newIndex);
		}
	}

	private void RefreshRowIfValid(int rowIndex)
	{
		if (rowIndex < 0 || rowIndex >= _table.Rows.Count)
		{
			return;
		}

		FormatRowCells(rowIndex);
		_table.InvalidateRow(rowIndex);
	}

	private void FormatAllCells()
	{
		for (var row = 0; row < _table.RowCount; row++)
		{
			FormatRowCells(row);
		}
	}

	private void FormatRowCells(int rowIndex)
	{
		for (var col = 0; col < _table.ColumnCount; col++)
		{
			ApplyCellFormattingSafe(rowIndex, col);
		}
	}

	private void ApplyCellFormattingSafe(int rowIndex, int columnIndex, DataGridViewCellStyle? targetStyle = null)
	{
		if (_table.InvokeRequired)
		{
			InvokeOnUiThread(() => ApplyCellFormatting(rowIndex, columnIndex, targetStyle));
		}
		else
		{
			ApplyCellFormatting(rowIndex, columnIndex, targetStyle);
		}
	}

	private void ApplyCellFormatting(int rowIndex, int columnIndex, DataGridViewCellStyle? targetStyle = null)
	{
		if (!_formattingEngine.IsValidCellCoordinate(rowIndex, columnIndex))
		{
			return;
		}

		if (_table.IsDisposed || !_table.IsHandleCreated)
		{
			return;
		}

		try
		{
			var visual = _formattingEngine.ResolveCellVisualState(rowIndex, columnIndex);
			_formattingEngine.ApplyVisualStateToCell(rowIndex, columnIndex, visual, targetStyle);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to apply formatting for cell [{Row},{Column}]", rowIndex, columnIndex);
		}
	}

	private void InvokeOnUiThread(Action action)
	{
		if (_table.IsDisposed || !_table.IsHandleCreated)
		{
			return;
		}

		if (_table.InvokeRequired)
		{
			try
			{
				_table.BeginInvoke(action);
			}
			catch (ObjectDisposedException ex)
			{
				_logger.LogDebug(ex, "Cannot invoke on UI thread - control already disposed");
			}
		}
		else
		{
			action();
		}
	}
}
