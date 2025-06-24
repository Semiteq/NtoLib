using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table.UI.TableUpdate
{
    public class UpdateBatcher
    {
        private readonly DataGridView _dataGridView;
        private readonly TableSchema _tableSchema;

        private readonly ConcurrentDictionary<(int row, int column), byte> _pendingUpdates = new();
        private volatile bool _isProcessing;

        public UpdateBatcher(DataGridView dataGridView, TableSchema tableSchema)
        {
            _dataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));
            _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
        }

        public void RequestCellUpdate(int row, int column)
        {
            if (!IsValidCoordinate(row, column)) return;

            _pendingUpdates.TryAdd((row, column), 0);
            
            if (_tableSchema.GetColumnKeyByIndex(column) is ColumnKey.Duration)
            {
                RequestCascadingTimeUpdates(row);
            }
        }

        public void RequestRowUpdate(int startIndex, bool isInsert = true)
        {
            var affectedRows = isInsert
                ? Enumerable.Range(startIndex, _dataGridView.RowCount - startIndex)
                : Enumerable.Range(startIndex, Math.Max(0, _dataGridView.RowCount - startIndex));

            foreach (var row in affectedRows)
            {
                for (int col = 0; col < _dataGridView.ColumnCount; col++)
                {
                    if (_dataGridView.Columns[col].Visible)
                    {
                        RequestCellUpdate(row, col);
                    }
                }
            }
        }
        
        public void ProcessUpdates()
        {
            if (_isProcessing || _pendingUpdates.IsEmpty)
                return;

            _isProcessing = true;

            try
            {
                var updates = _pendingUpdates.Keys.ToList();
                _pendingUpdates.Clear();

                if (updates.Count == 0) return;

                // Приоритизация видимых обновлений
                var prioritizedUpdates = PrioritizeUpdates(updates);

                // _onCellsChanged.Invoke(prioritizedUpdates);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private List<(int row, int column)> PrioritizeUpdates(List<(int row, int column)> updates)
        {
            var visible = new List<(int row, int column)>();
            var hidden = new List<(int row, int column)>();

            foreach (var (row, column) in updates)
            {
                if (IsCellVisible(row, column))
                    visible.Add((row, column));
                else
                    hidden.Add((row, column));
            }

            // Группируем по строкам для оптимизации
            var result = new List<(int row, int column)>();

            // Сначала видимые, сгруппированные по строкам
            result.AddRange(visible.OrderBy(x => x.row).ThenBy(x => x.column));

            // Потом скрытые
            result.AddRange(hidden.OrderBy(x => x.row).ThenBy(x => x.column));

            return result;
        }

        private bool IsCellVisible(int row, int column)
        {
            if (row < 0 || column < 0 || row >= _dataGridView.RowCount || column >= _dataGridView.ColumnCount)
                return false;

            var rect = _dataGridView.GetCellDisplayRectangle(column, row, false);
            return rect.Width > 0 && rect.Height > 0;
        }

        private void RequestCascadingTimeUpdates(int fromRow)
        {
            var timeColumnIndex = _tableSchema.GetIndexByColumnKey(ColumnKey.Duration);
            
            for (int row = fromRow + 1; row < _dataGridView.RowCount; row++)
            {
                _pendingUpdates.TryAdd((row, timeColumnIndex), 0);
            }
        }

        private bool IsValidCoordinate(int row, int column) =>
            row >= 0 && row < _dataGridView.RowCount &&
            column >= 0 && column < _dataGridView.ColumnCount;
        
        private bool IsValidRow(int row) =>
            row >= 0 && row < _dataGridView.RowCount;

        public void ClearPendingUpdates()
        {
            _pendingUpdates.Clear();
        }
        
        public bool HasPendingUpdates => !_pendingUpdates.IsEmpty;

        public void Dispose()
        {
            _pendingUpdates?.Clear();
        }
    }
}