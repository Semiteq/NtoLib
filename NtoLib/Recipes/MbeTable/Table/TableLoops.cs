using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions.TableLines;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private bool CheckRecipeCycles(List<RecipeLine> recipe)
        {
            var cycleDepth = 0;
            foreach (var recipeLine in recipe)
            {
                if (cycleDepth > Params.MaxLoopCount)
                    return false;
                switch (recipeLine)
                {
                    case For_Loop:
                        recipeLine.TabulateLevel = cycleDepth;
                        cycleDepth++;
                        break;
                    case EndFor_Loop:
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

        private void Tabulate()
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

                dataGridView1.Rows[i].HeaderCell.Value = tabulatorString + (i + 1).ToString();

                dataGridView1.Rows[i].Cells[0].Style.BackColor = tabulatorLevel switch
                {
                    0 => Color.White,
                    1 => Color.LightBlue,
                    2 => Color.LightSkyBlue,
                    3 => Color.DodgerBlue,
                    _ => dataGridView1.Rows[i].Cells[0].Style.BackColor
                };
            }
        }

        private void OnRowHeaderDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_tableData[e.RowIndex] is For_Loop)
            {
                var startIndex = e.RowIndex + 1;
                var endIndex = FindCycleEnd(startIndex);

                if (dataGridView1.Rows[startIndex].Height != ROW_HEIGHT)
                    ExpandRows(startIndex, endIndex);
                else
                    CollapseRows(startIndex, endIndex);
            }
        }

        private void CollapseRows(int startIndex, int endIndex)
        {
            dataGridView1.Rows[startIndex - 1].HeaderCell.Value = startIndex + " (+)";

            for (var i = startIndex; i <= endIndex; i++)
            {
                dataGridView1.Rows[i].Height = 1;
            }
        }
        private void ExpandRows(int startIndex, int endIndex)
        {
            dataGridView1.Rows[startIndex - 1].HeaderCell.Value = startIndex + " (-)";
            for (var i = startIndex; i <= endIndex; i++)
            {
                dataGridView1.Rows[i].Height = ROW_HEIGHT;
            }
        }

        private int FindCycleStart(int endIndex)
        {
            var tabulateLevel = 1;
            for (var i = endIndex - 1; i >= 0; i--)
            {
                switch (_tableData[i])
                {
                    case EndFor_Loop:
                        tabulateLevel++;
                        break;
                    case For_Loop:
                        tabulateLevel--;
                        break;
                }

                if (tabulateLevel == 0)
                    return i;
            }
            return -1;
        }
        private int FindCycleEnd(int startIndex)
        {
            var tabulateLevel = 1;
            for (var i = startIndex; i < _tableData.Count; i++)
            {
                switch (_tableData[i])
                {
                    case For_Loop:
                        tabulateLevel++;
                        break;
                    case EndFor_Loop:
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