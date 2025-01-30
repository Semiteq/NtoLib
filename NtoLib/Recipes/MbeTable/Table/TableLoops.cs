using NtoLib.Recipes.MbeTable.TableLines;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private bool CheckRecipeCycles()
        {
            int cycleDepth = 0;
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
            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            cellStyle.BackColor = Color.Coral;
            cellStyle.ForeColor = Color.Black;
            cellStyle.SelectionBackColor = Color.Chocolate;

            for (int i = 0; i < _tableData.Count; i++)
            {
                string tabulatorString = string.Empty;

                int tabulatorLevel = 0;
                for (tabulatorLevel = 0; tabulatorLevel < _tableData[i].tabulateLevel; tabulatorLevel++)
                    tabulatorString += "\t";

                dataGridView1.Rows[i].HeaderCell.Value = tabulatorString + (i + 1).ToString();

                if (tabulatorLevel == 0)
                    dataGridView1.Rows[i].Cells[0].Style.BackColor = Color.White;
                else if (tabulatorLevel == 1)
                    dataGridView1.Rows[i].Cells[0].Style.BackColor = Color.LightBlue;
                else if (tabulatorLevel == 2)
                    dataGridView1.Rows[i].Cells[0].Style.BackColor = Color.LightSkyBlue;
                else if (tabulatorLevel == 3)
                    dataGridView1.Rows[i].Cells[0].Style.BackColor = Color.DodgerBlue;
            }
        }

        private void OnRowHeaderDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_tableData[e.RowIndex] is For_Loop)
            {
                int startIndex = e.RowIndex + 1;
                int endIndex = FindCycleEnd(startIndex);

                if (dataGridView1.Rows[startIndex].Height != ROW_HEIGHT)
                    ExpandRows(startIndex, endIndex);
                else
                    CollapseRows(startIndex, endIndex);
            }
        }

        private void CollapseRows(int startIndex, int endIndex)
        {
            dataGridView1.Rows[startIndex - 1].HeaderCell.Value = startIndex + " (+)";

            for (int i = startIndex; i <= endIndex; i++)
            {
                dataGridView1.Rows[i].Height = 1;
            }
        }
        private void ExpandRows(int startIndex, int endIndex)
        {
            dataGridView1.Rows[startIndex - 1].HeaderCell.Value = startIndex + " (-)";
            for (int i = startIndex; i <= endIndex; i++)
            {
                dataGridView1.Rows[i].Height = ROW_HEIGHT;
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
        private int FindCycleEnd(int startIndex)
        {
            int tabulateLevel = 1;
            for (int i = startIndex; i < _tableData.Count; i++)
            {
                if (_tableData[i] is For_Loop)
                    tabulateLevel++;
                else if (_tableData[i] is EndFor_Loop)
                    tabulateLevel--;

                if (tabulateLevel == 0)
                    return i - 1;
            }
            return -1;
        }
    }
}