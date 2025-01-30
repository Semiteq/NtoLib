using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.TableLines;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{
    internal class RecipeTime
    {
        private uint totalTimeLeft = 0;
        private uint lineTimeLeft = 0;

        private List<RecipeLine> _tableData;
        private DataGridView _dataGridView;

        private TimeSpan time;

        public RecipeTime(List<RecipeLine> TableData, DataGridView DataGridView) 
        {
            _tableData = TableData;
            _dataGridView = DataGridView;
        }

        private void RecalculateTime()
        {
            if (_tableData.Count == 0) return;

            _tableData[0].CycleTime = 0f;
            _dataGridView.Rows[0].Cells[Params.RecipeTimeIndex].Value = TimeSpan.Zero.ToString("hh\\:mm\\:ss\\.ff");

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
                    _dataGridView.Rows[rowIndex + 1].Cells[Params.RecipeTimeIndex].Value = time.ToString("hh\\:mm\\:ss\\.ff");
                }
            }
        }

        private int FindCycleStart(int endIndex)
        {
            int tabulateLevel = 1;
            for (int i = endIndex - 1; i >= 0; i--)
            {
                if (_tableData[i] is EndFor_Loop)
                    tabulateLevel++;
                else if (_tableData[i] is For_Loop)
                    tabulateLevel--;

                if (tabulateLevel == 0)
                    return i;
            }
            return -1;
        }

        public void CalculateTotalTime(List<RecipeLine> table)
        {
            uint sum = 0;

            foreach (var row in table)
            {
                var value = row.GetCells[Params.RecipeTimeIndex].GetValue();
                if (uint.TryParse(value.ToString(), out uint result))
                {
                    sum += result;
                }
            }
            totalTimeLeft = sum * 1000;
        }

        public void CalculateLineTime(List<RecipeLine> table, int rowNumber)
        {
            uint sum = 0;

            var value = table[rowNumber].GetCells[Params.RecipeTimeIndex].GetValue();
            if (uint.TryParse(value.ToString(), out uint result))
            {
                sum = result * 1000;
            }

            lineTimeLeft = sum * 1000;
        }
    }
}
