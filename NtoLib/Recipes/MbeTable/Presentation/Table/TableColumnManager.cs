using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table
{
    public class TableColumnManager
    {
        private readonly DataGridView _table;
        private readonly TableSchema _tableSchema;
        private readonly ColorScheme _colorScheme;
        private readonly RecipeViewModel _recipeViewModel;
        
        private readonly IReadOnlyDictionary<ColumnKey, IColumnFactory> _factories;

        public TableColumnManager(DataGridView table, 
            TableSchema tableSchema, 
            IReadOnlyDictionary<ColumnKey, IColumnFactory> factories,
            ColorScheme colorScheme, 
            RecipeViewModel recipeViewModel)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
            _factories  = factories ?? throw new ArgumentNullException(nameof(factories));
            _colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));
            _recipeViewModel = recipeViewModel ?? throw new ArgumentNullException(nameof(recipeViewModel));
            
        }

        public void InitializeHeaders()
        {
            _table.ColumnHeadersDefaultCellStyle.Font = _colorScheme.HeaderFont;
            _table.ColumnHeadersDefaultCellStyle.BackColor = _colorScheme.HeaderBgColor;
            _table.ColumnHeadersDefaultCellStyle.ForeColor = _colorScheme.HeaderTextColor;
            _table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            _table.RowHeadersVisible = true;
            _table.RowHeadersWidth = 80;

            _table.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _table.RowHeadersDefaultCellStyle.Font = _colorScheme.HeaderFont;
            _table.RowHeadersDefaultCellStyle.BackColor = _colorScheme.HeaderBgColor;
            _table.RowHeadersDefaultCellStyle.ForeColor = _colorScheme.HeaderTextColor;

            _table.EnableHeadersVisualStyles = false;
        }

        public void InitializeTableColumns()
        {
            _table.AutoGenerateColumns = false;
            _table.Columns.Clear();

            var defaultFactory = new TextBoxColumnFactory();
            
            foreach (var colDef in _tableSchema.GetColumns())
            {
                _factories.TryGetValue(colDef.Key, out var factory);
                factory ??= defaultFactory;
                
                var column = factory.CreateColumn(colDef, _colorScheme);
                _table.Columns.Add(column);
            }

            _table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _table.AllowUserToAddRows = false;
            _table.AllowUserToDeleteRows = false;
            _table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _table.MultiSelect = false;
        }

        public void InitializeTableRows()
        {
            _table.Rows.Clear();
            _table.RowTemplate.Height = _colorScheme.LineFont.Height + 8;
        }
    }
}