using System.Windows.Forms;
using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.TableLines;

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

            RecalculateTime();
        }

        private void RecalculateTime()
        {
            if (_tableData.Count == 0) return;

            _tableData[0].CycleTime = 0f;
            dataGridView1.Rows[0].Cells[Params.RecipeTimeIndex].Value = TimeSpan.Zero.ToString("hh\\:mm\\:ss\\.ff");

            TimeSpan time = TimeSpan.Zero;

            for (int rowIndex = 0; rowIndex < _tableData.Count; rowIndex++)
            {
                RecipeLine recipeLine = _tableData[rowIndex];

                if (recipeLine is EndFor_Loop)
                {
                    int cycleStartIndex = FindCycleStart(rowIndex);
                    float cycleTime = (recipeLine.CycleTime - _tableData[cycleStartIndex].CycleTime) * (_tableData[cycleStartIndex].GetSetpoint() - 1);
                    time += TimeSpan.FromSeconds(cycleTime);
                }
                else if (recipeLine.ActionTime == ActionTime.TimeSetpoint)
                {
                    time += TimeSpan.FromSeconds(recipeLine.GetTime());
                }

                if (rowIndex < _tableData.Count - 1)
                {
                    _tableData[rowIndex + 1].CycleTime = (float)time.TotalSeconds;
                    dataGridView1.Rows[rowIndex + 1].Cells[Params.RecipeTimeIndex].Value = time.ToString("hh\\:mm\\:ss\\.ff");
                }
            }
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

            GrowthList.Instance.UpdateNames();

            try
            {
                AddLineToRecipe(factory.NewLine("CLOSE", GrowthList.Instance.GetMinShutter(), 0f, 0f, ""), false);
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

            GrowthList.Instance.UpdateNames();

            try
            {
                AddLineToRecipe(factory.NewLine("CLOSE", GrowthList.Instance.GetMinShutter(), 0f, 0f, ""), true);
                RefreshTable();
            }
            catch (InvalidOperationException ex)
            {
                WriteStatusMessage($"Ошибка состаления списка аргументов: {ex.Message}", true);
            }
        }
    }
}
