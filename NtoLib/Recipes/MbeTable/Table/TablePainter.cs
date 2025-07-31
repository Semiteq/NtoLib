using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class TablePainter
    {
        public enum StateType { Default, Selected, Passed, Blocked }

        private readonly ColorScheme _colorScheme;
        private readonly Font _blockedFont; 

        public TablePainter(ColorScheme colorScheme)
        {
            _colorScheme = colorScheme;
            // Можно вынести в ColorScheme
            _blockedFont = new Font("Arial", 14f, FontStyle.Italic);
        }

        public StateType GetStateType(StepViewModel viewModel, int actualLineNumber, ColumnKey columnKey)
        {
            
            if (viewModel.IsCellBlocked(columnKey))
            {
                return StateType.Blocked;
            }

            if (viewModel.RowIndex < actualLineNumber)
            {
                return StateType.Passed;
            }

            if (viewModel.RowIndex == actualLineNumber)
            {
                return StateType.Selected;
            }

            return StateType.Default;
        }

        public void ApplyState(DataGridViewCellStyle cellStyle, StateType state)
        {
            Font font;
            Color foreColor;
            Color backColor;

            switch (state)
            {
                case StateType.Selected:
                    font = _colorScheme.SelectedLineFont;
                    foreColor = _colorScheme.SelectedLineTextColor;
                    backColor = _colorScheme.SelectedLineBgColor;
                    break;
                case StateType.Passed:
                    font = _colorScheme.PassedLineFont;
                    foreColor = _colorScheme.PassedLineTextColor;
                    backColor = _colorScheme.PassedLineBgColor;
                    break;
                case StateType.Blocked:
                    font = _blockedFont;
                    foreColor = Color.DarkGray; // Можно вынести в ColorScheme
                    backColor = Color.LightGray; // Можно вынести в ColorScheme
                    break;
                case StateType.Default:
                default:
                    font = _colorScheme.LineFont;
                    foreColor = _colorScheme.LineTextColor;
                    backColor = _colorScheme.LineBgColor;
                    break;
            }

            if (!Equals(cellStyle.Font, font)) cellStyle.Font = font;
            if (cellStyle.ForeColor != foreColor) cellStyle.ForeColor = foreColor;
            if (cellStyle.BackColor != backColor) cellStyle.BackColor = backColor;
        }
    }
}