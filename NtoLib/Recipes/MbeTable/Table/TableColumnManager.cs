using System;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.RecipeManager.ViewModels;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class TableColumnManager
    {
        private readonly DataGridView _table;
        private readonly TableSchema _tableSchema;
        private readonly ColorScheme _colorScheme;
        private readonly RecipeViewModel _recipeViewModel;

        public TableColumnManager(DataGridView table, TableSchema tableSchema, ColorScheme colorScheme, RecipeViewModel recipeViewModel)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
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

            _table.EnableHeadersVisualStyles = false;
        }

        public void InitializeTableColumns()
        {
            _table.AutoGenerateColumns = false;
            _table.Columns.Clear();

            foreach (var colDef in _tableSchema.GetReadonlyColumns())
            {
                DataGridViewColumn column;

                
                column = new DataGridViewTextBoxColumn();
                column.DataPropertyName = $"Item[{colDef.Key}]";
                

                column.Name = colDef.Key.ToString();
                column.HeaderText = colDef.UiName;



                column.ReadOnly = colDef.ReadOnly;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;

                if (colDef.Width > 0)
                {
                    column.Width = colDef.Width;
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
                else
                {
                    //column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                column.DefaultCellStyle.Alignment = colDef.Alignment;
                column.DefaultCellStyle.Font = _colorScheme.LineFont;
                column.DefaultCellStyle.BackColor = _colorScheme.LineBgColor;
                column.DefaultCellStyle.ForeColor = _colorScheme.LineTextColor;

                _table.Columns.Add(column);
            }

            _table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _table.AllowUserToAddRows = false;
            _table.AllowUserToDeleteRows = false;
            _table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _table.MultiSelect = false;
        }
    }
}