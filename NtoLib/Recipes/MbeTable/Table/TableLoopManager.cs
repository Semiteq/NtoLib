using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class TableLoopManager
    {
        private const int RowHeight = 32;
        private const int MaxLoopCount = 3;
        private readonly DataGridView _dataGridView;
        private readonly List<Step> _tableData;

        public TableLoopManager(DataGridView dataGridView, List<Step> tableData)
        {
            _dataGridView = dataGridView;
            _tableData = tableData;
        }

        public bool CheckRecipeCycles(List<Step> recipe)
        {
            var cycleDepth = 0;
            foreach (var recipeLine in recipe)
            {
                if (cycleDepth > MaxLoopCount)
                    return false;
                switch (recipeLine.ActionEntry)
                {
                    case ForLoop:
                        recipeLine.TabulateLevel = cycleDepth;
                        cycleDepth++;
                        break;
                    case EndForLoop:
                        {
                            cycleDepth--;

                            if (cycleDepth < 0)
                                return false;

                            recipeLine.TabulateLevel = cycleDepth;
                            break;
                        }
                    default:
                        recipeLine.TabulateLevel = cycleDepth;
                        break;
                }
            }

            Tabulate();
            return cycleDepth == 0;
        }

        public void Tabulate()
        {
            var cellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.Coral,
                ForeColor = Color.Black,
                SelectionBackColor = Color.Chocolate
            };

            for (var i = 0; i < _tableData.Count; i++)
            {
                var tabulatorString = string.Empty;
                var tabulatorLevel = 0;
                
                for (tabulatorLevel = 0; tabulatorLevel < _tableData[i].TabulateLevel; tabulatorLevel++)
                    tabulatorString += "\t";

                _dataGridView.Rows[i].HeaderCell.Value = tabulatorString + (i + 1).ToString();

                _dataGridView.Rows[i].Cells[0].Style.BackColor = tabulatorLevel switch
                {
                    0 => Color.White,
                    1 => Color.LightBlue,
                    2 => Color.LightSkyBlue,
                    3 => Color.DodgerBlue,
                    _ => _dataGridView.Rows[i].Cells[0].Style.BackColor
                };
            }
        }

        public void OnRowHeaderDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_tableData[e.RowIndex] is ForLoop)
            {
                var startIndex = e.RowIndex + 1;
                var endIndex = FindCycleEnd(startIndex);

                if (_dataGridView.Rows[startIndex].Height != RowHeight)
                    ExpandRows(startIndex, endIndex);
                else
                    CollapseRows(startIndex, endIndex);
            }
        }

        private void CollapseRows(int startIndex, int endIndex)
        {
            _dataGridView.Rows[startIndex - 1].HeaderCell.Value = startIndex + " (+)";

            for (var i = startIndex; i <= endIndex; i++)
            {
                _dataGridView.Rows[i].Height = 1;
            }
        }
        
        private void ExpandRows(int startIndex, int endIndex)
        {
            _dataGridView.Rows[startIndex - 1].HeaderCell.Value = startIndex + " (-)";
            for (var i = startIndex; i <= endIndex; i++)
            {
                _dataGridView.Rows[i].Height = RowHeight;
            }
        }

        public int FindCycleStart(int endIndex)
        {
            var tabulateLevel = 1;
            for (var i = endIndex - 1; i >= 0; i--)
            {
                switch (_tableData[i])
                {
                    case EndForLoop:
                        tabulateLevel++;
                        break;
                    case ForLoop:
                        tabulateLevel--;
                        break;
                }

                if (tabulateLevel == 0)
                    return i;
            }
            return -1;
        }
        
        public int FindCycleEnd(int startIndex)
        {
            var tabulateLevel = 1;
            for (var i = startIndex; i < _tableData.Count; i++)
            {
                switch (_tableData[i])
                {
                    case ForLoop:
                        tabulateLevel++;
                        break;
                    case EndForLoop:
                        tabulateLevel--;
                        break;
                }

                if (tabulateLevel == 0)
                    return i - 1;
            }
            return -1;
        }
    }
}