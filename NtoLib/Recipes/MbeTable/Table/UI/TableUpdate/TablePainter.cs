using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MasterSCADA.Interfaces;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table.UI.TableUpdate;

/// <summary>
/// Class responsible for painting the DataGridView cells based on the current state of the recipe steps.
/// </summary>
public class TablePainter
{
    private readonly DataGridView _dataGridView;
    private readonly Dictionary<ColumnKey, int> _columnKeyToIndexMap;
    private readonly ColorScheme _colorScheme;

    private readonly CellState _stateDefault;
    private readonly CellState _stateSelected;
    private readonly CellState _statePassed;
    private readonly CellState _stateBlocked;

    public TablePainter(DataGridView dataGridView, Dictionary<ColumnKey, int> columnKeyToIndexMap,
        ColorScheme colorScheme)
    {
        _dataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));
        _columnKeyToIndexMap = columnKeyToIndexMap ?? throw new ArgumentNullException(nameof(columnKeyToIndexMap));
        _colorScheme = colorScheme;

        _stateDefault = new(_colorScheme.LineFont, _colorScheme.LineTextColor, _colorScheme.LineBgColor);
        _stateSelected = new(_colorScheme.SelectedLineFont, _colorScheme.SelectedLineTextColor, _colorScheme.SelectedLineBgColor);
        _statePassed = new(_colorScheme.PassedLineFont, _colorScheme.PassedLineTextColor, _colorScheme.PassedLineBgColor);
        _stateBlocked = new(new Font("Arial", 14f), Color.DarkGray, Color.LightGray, true);
    }

    private bool ValidateCellCoordinates(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _dataGridView.Rows.Count) return false;
        if (columnIndex < 0 || columnIndex >= _dataGridView.Columns.Count) return false;
        return true;
    }

    private void PaintCell(int rowIndex, int columnIndex, CellState state)
    {
        if (!ValidateCellCoordinates(rowIndex, columnIndex)) return;

        var cell = _dataGridView.Rows[rowIndex].Cells[columnIndex];
        state.ApplyTo(cell);
    }

    private CellState GetCellState(int rowIndex, int columnIndex, Step step)
    {
        if (!ValidateCellCoordinates(rowIndex, columnIndex)) return _stateDefault;

        var currentColumnKey = _columnKeyToIndexMap
            .FirstOrDefault(kvp => kvp.Value == columnIndex).Key;

        if (step.TryGetProperty(currentColumnKey).IsBlocked)
            return _stateBlocked;

        return _stateDefault;
    }

    private CellState GetCellStateProgress(int rowIndex, int columnIndex, int actualLineNumber, Step step)
    {
        if (!ValidateCellCoordinates(rowIndex, columnIndex)) return _stateDefault;

        if (rowIndex < actualLineNumber)
            return _statePassed;

        if (rowIndex == actualLineNumber)
            return _stateSelected;

        var currentColumnKey = _columnKeyToIndexMap
            .FirstOrDefault(kvp => kvp.Value == columnIndex).Key;

        if (step.TryGetProperty(currentColumnKey).IsBlocked)
            return _stateBlocked;

        return _stateDefault;
    }

    public void PaintRow(int rowIndex, Step step)
    {
        if (rowIndex < 0 || rowIndex >= _dataGridView.Rows.Count) return;

        var row = _dataGridView.Rows[rowIndex];
        foreach (DataGridViewCell cell in row.Cells)
        {
            var cellState = GetCellState(rowIndex, cell.ColumnIndex, step);
            cellState.ApplyTo(cell);
        }
    }

    public void PaintRowProgress(int row, int actualLineNumber, Step step)
    {
        if (_dataGridView.Rows.Count == 0) return;
        PaintRowProgress(row, actualLineNumber, step);
    }

    public void ClearStyles()
    {
        _dataGridView.SuspendLayout();
        try
        {
            foreach (DataGridViewRow row in _dataGridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    _stateDefault.ApplyTo(cell);
                }
            }
        }
        finally
        {
            _dataGridView.ResumeLayout(true);
            _dataGridView.Refresh();
        }
    }
}