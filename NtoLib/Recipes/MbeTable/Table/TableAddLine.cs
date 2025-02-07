using System.Windows.Forms;
using System;
using FB.VisualFB;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private void AddLineToRecipe(RecipeLine recipeLine, bool addAfter)
        {
            int rowIndex = dataGridView1?.CurrentRow?.Index ?? 0;
            int insertIndex = addAfter ? rowIndex + 1 : rowIndex;
            if (dataGridView1.RowCount == 0) insertIndex = 0;

            recipeLine.Row.Height = ROW_HEIGHT;

            _tableData.Insert(insertIndex, recipeLine);
            InsertRow(recipeLine.Row, insertIndex);
            RefreshRowSelection(insertIndex);
            BlockCells(insertIndex);
        }

        private void ReplaceLineInRecipe(RecipeLine recipeLine)
        {
            int rowIndex = dataGridView1.CurrentRow?.Index ?? 0;
            recipeLine.Row.Height = ROW_HEIGHT;

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
                WriteStatusMessage("Ошибка составления рецепта: Несоответствие команд FOR и END_FOR", true);
                return;
            }

            button_save.Enabled = true;

            if (_tableType == TableMode.Edit)
            {
                WriteStatusMessage("Редактирование рецепта: рецепт корректный", false);
            }

            RecipeTime.SetData(_tableData, dataGridView1);
            RecipeTime.Recalculate();

            VisualFBBase fb = FBConnector.Fb as MbeTableFB;

            fb.SetPinValue(Params.TotalTimeLeft, RecipeTime.TotalTime);
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

            int currentIndex = dataGridView1.CurrentRow.Index;
            _tableData.RemoveAt(currentIndex);
            dataGridView1.Rows.RemoveAt(currentIndex);

            RefreshRowHeaders();
            RefreshTable();
        }

        private void RefreshRowHeaders()
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                dataGridView1.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }

        private void ClickButton_AddLineBefore(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View) return;

            try
            {
                AddLineToRecipe(factory.NewLine("CLOSE", GrowthList.GetMinShutter(), 0f, 0f, string.Empty), false);
                RefreshTable();
            }
            catch (InvalidOperationException ex)
            {
                WriteStatusMessage($"Ошибка состаления списка аргументов: {ex.Message}", true);
            }
        }

        private void ClickButton_AddLineAfter(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View) return;

            try
            {
                AddLineToRecipe(factory.NewLine("CLOSE", GrowthList.GetMinShutter(), 0f, 0f, string.Empty), true);
                RefreshTable();
            }
            catch (InvalidOperationException ex)
            {
                WriteStatusMessage($"Ошибка состаления списка аргументов: {ex.Message}", true);
            }
        }
    }
}
