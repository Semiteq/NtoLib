#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories
{
    public class ActionComboBoxColumnFactory : IColumnFactory
    {
        private readonly IComboboxDataProvider _dataProvider;

        public ActionComboBoxColumnFactory(IComboboxDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
        {
            var comboColumn = new DataGridViewComboBoxColumn
            {
                Name = colDef.Key.ToString(),
                HeaderText = colDef.UiName,
                DataSource = _dataProvider.GetActions(),
                DisplayMember = "Value",
                ValueMember = "Key",
                ValueType = typeof(int?),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                DisplayStyleForCurrentCellOnly = true,
                FlatStyle = FlatStyle.Standard,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            comboColumn.DefaultCellStyle.Alignment = colDef.Alignment;
            comboColumn.DefaultCellStyle.Font = colorScheme.LineFont;
            comboColumn.DefaultCellStyle.BackColor = colorScheme.LineBgColor;

            comboColumn.MaxDropDownItems = 20;
            
            if (colDef.Width > 0)
            {
                comboColumn.Width = colDef.Width;
                comboColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }
            else
            {
                comboColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            return comboColumn;
        }
    }
}