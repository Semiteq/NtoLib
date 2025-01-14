using System.Windows.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.TableLines;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private void AddLineToRecipe(RecipeLine recipeLine, bool addAfter)
        {
            int rowIndex = dataGridView1?.CurrentRow?.Index ?? 0;
            int additionalIndex = (addAfter && _tableData.Count != 0) ? 1 : 0;

            List<RecipeLine> recipeLines = FormNewTableData(recipeLine, addAfter, rowIndex);

            recipeLine.Row.Height = ROW_HEIGHT;

            _tableData.Clear();
            _tableData = recipeLines;

            int index = rowIndex + additionalIndex;

            dataGridView1.Rows.Insert(index, recipeLine.Row);
            dataGridView1.Rows[index].Selected = true;
            dataGridView1.Rows[index].Cells[0].Selected = true;

            BlockCells(index);
        }

        private void RefreshTable()
        {
            if (!CheckRecipeCycles())
            {
                this.button_save.Enabled = false;
                WriteStatusMessage("Ошибка составления рецепта: " +
                                    "Не соответствие комманд FOR и END_FOR", true);
                return;
            }
            else
            {
                this.button_save.Enabled = true;

                string message = "";
                if (_tableType == TableMode.Edit)
                {
                    message = "Редактирование рецепта: рецепт корректный";
                    WriteStatusMessage(message, false);
                }
                //else
                //    message = "Просмотр рецепта";

                RecalculateTime();
            }
        }

        private List<RecipeLine> FormNewTableData(RecipeLine recipeLine, bool addAfter, int rowIndex)
        {
            List<RecipeLine> recipeLines = new List<RecipeLine>();
            int index = 0;

            foreach (RecipeLine line in _tableData)
            {
                if (addAfter)
                {
                    recipeLines.Add(line);
                    if (index == rowIndex)
                        recipeLines.Add(recipeLine);
                }
                else
                {
                    if (index == rowIndex)
                        recipeLines.Add(recipeLine);
                    recipeLines.Add(line);
                }
                ++index;
            }

            if (recipeLines.Count == 0)
                recipeLines.Add(recipeLine);
            return recipeLines;
        }

        private void RecalculateTime()
        {
            if (_tableData.Count > 0)
            {
                _tableData[0].CycleTime = 0f;
                dataGridView1.Rows[0].Cells[Params.RecipeTimeIndex].Value = TimeSpan.Zero.ToString(@"hh\:mm\:ss\.ff");
            }
            else
                return;

            TimeSpan time = TimeSpan.Zero;

            int cycleStartIndex = 0;

            for (int rowIndex = 0; rowIndex < _tableData.Count; rowIndex++)
            {
                RecipeLine recipeLine = _tableData[rowIndex];

                if (recipeLine is For_Loop)
                { 
                    float cycleTIme = 0f;
                }
                else if (recipeLine is EndFor_Loop)
                {
                    cycleStartIndex = FindCycleStart(rowIndex);

                    float cycleTime = (recipeLine.CycleTime - _tableData[cycleStartIndex].CycleTime) * (_tableData[cycleStartIndex].GetSetpoint() - 1);
                    time = time + TimeSpan.FromSeconds(cycleTime);
                }
                else if (recipeLine.ActionTime == ActionTime.TimeSetpoint)
                {
                    time = time + TimeSpan.FromSeconds(recipeLine.GetTime());
                }

                if (rowIndex < _tableData.Count - 1)
                {
                    _tableData[rowIndex + 1].CycleTime = (float)time.TotalSeconds;
                    dataGridView1.Rows[rowIndex + 1].Cells[Params.RecipeTimeIndex].Value = time.ToString(@"hh\:mm\:ss\.ff");
                }
            }
        }

        private void ClickButton_Delete(object sender, EventArgs e)
        {
            if (this.FBConnector.DesignMode || _tableType == TableMode.View || dataGridView1.CurrentRow == null)
                return;

            _tableData.RemoveAt(dataGridView1.CurrentRow.Index);
            dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);

            int num = 1;
            foreach (DataGridViewRow row in (IEnumerable)dataGridView1.Rows)
            {
                row.HeaderCell.Value = (object)num.ToString();
                ++num;
            }
            RefreshTable();
        }
        private void ClickButton_AddLineBefore(object sender, EventArgs e)
        {
            if (this.FBConnector.DesignMode || _tableType == TableMode.View)
                return;

            AddLineToRecipe(factory.NewLine("CLOSE", 0, 0f, 0f, ""), false);
            RefreshTable();
        }
        private void ClickButton_AddLineAfter(object sender, EventArgs e)
        {
            if (this.FBConnector.DesignMode || _tableType == TableMode.View)
                return;

            AddLineToRecipe(factory.NewLine("CLOSE", 0, 0f, 0f, ""), true);
            RefreshTable();
        }
    }
}