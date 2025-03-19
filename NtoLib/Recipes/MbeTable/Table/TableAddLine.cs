using System;
using System.Linq;
using System.Windows.Forms;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.RecipeLines;
using NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        public static IRecipeTimeManager RecipeTimeManager;
        private void AddLineToRecipe(RecipeLine recipeLine, bool addAfter)
        {
            var rowIndex = dataGridView1?.CurrentRow?.Index ?? 0;
            var insertIndex = addAfter ? rowIndex + 1 : rowIndex;
            if (dataGridView1.RowCount == 0) insertIndex = 0;

            recipeLine.Row.Height = TableControl.ROW_HEIGHT;

            _tableData.Insert(insertIndex, recipeLine);
            InsertRow(recipeLine.Row, insertIndex);
            RefreshRowSelection(insertIndex);
            BlockCells(insertIndex);
        }

        private void ReplaceLineInRecipe(RecipeLine recipeLine)
        {
            var rowIndex = dataGridView1.CurrentRow?.Index ?? 0;
            recipeLine.Row.Height = TableControl.ROW_HEIGHT;

            _tableData[rowIndex] = recipeLine;

            dataGridView1.Rows.RemoveAt(rowIndex);
            InsertRow(recipeLine.Row, rowIndex);

            RefreshRowSelection(rowIndex);
            BlockCells(rowIndex);
        }

        private void RefreshTable()
        {
            if (!CheckRecipeCycles())
            {
                button_save.Enabled = false;
                StatusManager.WriteStatusMessage("Ошибка составления рецепта: Несоответствие команд FOR и END_FOR", true);
                return;
            }

            button_save.Enabled = true;

            if (_tableType == TableMode.Edit)
            {
                StatusManager.WriteStatusMessage("Редактирование рецепта: рецепт корректный", false);
            }

            RecipeTimeManager.SetData(_tableData, dataGridView1);
            RecipeTimeManager.Recalculate();

            VisualFBBase fb = FBConnector.Fb as MbeTableFB;

            fb.SetPinValue(Params.TotalTimeLeft, RecipeTimeManager.TotalTime);

            dataGridView1.Refresh();
        }

        private void InsertRow(DataGridViewRow row, int index)
        {
            dataGridView1.Rows.Insert(index, row);
        }

        private void RefreshRowSelection(int index)
        {
            dataGridView1.ClearSelection();
            dataGridView1.Rows[index].Selected = true;
            dataGridView1.Rows[index].Cells[0].Selected = true;
        }

        private void ClickButton_Delete(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View || dataGridView1.CurrentRow == null) return;

            var currentIndex = dataGridView1.CurrentRow.Index;
            _tableData.RemoveAt(currentIndex);
            dataGridView1.Rows.RemoveAt(currentIndex);

            RefreshRowHeaders();
            RefreshTable();
        }

        private void RefreshRowHeaders()
        {
            for (var i = 0; i < dataGridView1.Rows.Count; i++)
            {
                dataGridView1.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }

        private void ClickButton_AddLineBefore(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View) return;

            try
            {
                AddLineToRecipe(RecipeLineFactory.NewLine("CLOSE", ActionTarget.ShutterNames.FirstOrDefault().Key, 0f, 0f, 0f, 0f, string.Empty), true);
                RefreshTable();
            }
            catch (InvalidOperationException ex)
            {
                StatusManager.WriteStatusMessage($"Ошибка состаления списка аргументов: {ex.Message}", true);
            }
        }

        private void ClickButton_AddLineAfter(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View) return;

            try
            {
                AddLineToRecipe(RecipeLineFactory.NewLine("CLOSE", ActionTarget.ShutterNames.FirstOrDefault().Key, 0f, 0f, 0f, 0f, string.Empty), true);
                RefreshTable();
            }
            catch (InvalidOperationException ex)
            {
                StatusManager.WriteStatusMessage($"Ошибка состаления списка аргументов: {ex.Message}", true);
            }
        }
    }
}
