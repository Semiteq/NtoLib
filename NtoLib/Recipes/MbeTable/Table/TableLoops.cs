using NtoLib.Recipes.MbeTable.TableLines;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private bool CheckRecipeCycles()
        {
            var cycleDepth = 0;
            foreach (var recipeLine in _tableData)
            {
                if (recipeLine is For_Loop)
                {
                    recipeLine.tabulateLevel = cycleDepth;
                    cycleDepth++;
                }
                else if (recipeLine is EndFor_Loop)
                {
                    cycleDepth--;

                    if (cycleDepth < 0)
                        return false;

                    recipeLine.tabulateLevel = cycleDepth;
                }
                else
                {
                    recipeLine.tabulateLevel = cycleDepth;
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
                for (tabulatorLevel = 0; tabulatorLevel < _tableData[i].tabulateLevel; tabulatorLevel++)
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