#nullable enable

using System;
using System.Windows.Forms;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.VirtualMode;

/// <summary>
/// Manages DataGridView VirtualMode data requests by routing to RecipeViewModel.
/// Handles cell value retrieval, validation, and selective invalidation.
/// </summary>
public sealed class VirtualModeDataManager
{
    private readonly RecipeViewModel _recipeViewModel;
    private readonly TableColumns _columns;
    private readonly DataGridView _dataGridView;
    private readonly ILogger _logger;

    public VirtualModeDataManager(
        RecipeViewModel recipeViewModel,
        TableColumns columns,
        DataGridView dataGridView,
        ILogger logger)
    {
        _recipeViewModel = recipeViewModel ?? throw new ArgumentNullException(nameof(recipeViewModel));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _dataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called by CellValueNeeded. Returns display value for the cell.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnIndex">Zero-based column index.</param>
    /// <returns>Formatted value for display in the cell.</returns>
    public object? GetCellValue(int rowIndex, int columnIndex)
    {
        return _recipeViewModel.GetCellValue(rowIndex, columnIndex);
    }

    /// <summary>
    /// Called by CellValuePushed. Validates and commits user input.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnIndex">Zero-based column index.</param>
    /// <param name="value">User-provided value from editing control.</param>
    /// <returns>Result indicating success or validation error.</returns>
    public Result SetCellValue(int rowIndex, int columnIndex, object? value)
    {
        var result = _recipeViewModel.SetCellValue(rowIndex, columnIndex, value);
        
        if (result.IsFailed)
            InvalidateCell(rowIndex, columnIndex);
        
        return result;
    }

    /// <summary>
    /// Invalidates a specific row in the DataGridView, forcing repaint.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    public void InvalidateRow(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < _dataGridView.Rows.Count)
        {
            _dataGridView.InvalidateRow(rowIndex);
        }
    }

    /// <summary>
    /// Invalidates a specific cell in the DataGridView.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnIndex">Zero-based column index.</param>
    public void InvalidateCell(int rowIndex, int columnIndex)
    {
        if (rowIndex >= 0 && rowIndex < _dataGridView.Rows.Count &&
            columnIndex >= 0 && columnIndex < _dataGridView.Columns.Count)
        {
            _dataGridView.InvalidateCell(columnIndex, rowIndex);
        }
    }

    /// <summary>
    /// Updates RowCount after structural changes (add/remove rows).
    /// </summary>
    public void RefreshRowCount()
    {
        var count = _recipeViewModel.GetRowCount();
        _dataGridView.RowCount = count;
    }
}