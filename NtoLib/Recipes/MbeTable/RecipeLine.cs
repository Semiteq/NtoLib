using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{
    internal abstract class RecipeLine
    {
        public List<TCell> GetCells => _cells;

        protected List<TCell> _cells = new();

        //Номер вложенного цикла todo: убрать в TableLoops
        public int tabulateLevel = 0;

        public int MinNumber { get; private set; } = 1;
        public int MaxNumber { get; private set; } = 16;

        public float MinSetpoint { get; protected set; }
        public float MaxSetpoint { get; protected set; }

        public float MinTimeSetpoint { get; protected set; } = 0.1f;
        public float MaxTimeSetpoint { get; protected set; } = 10800;

        public abstract ActionTime ActionTime { get; }

        public DataGridViewRow Row;

        protected string shutterName;
        protected string heaterName;

        protected RecipeLine(string name)
        {
            // Заполненение новой строки данными слева направо
            Row = new DataGridViewRow();

            Row.Cells.Add(ActionCell);
            Row.Cells[Params.CommandIndex].Value = name;

            if (GrowthList.Instance.GetTargetAction(name) == "shutter")
                Row.Cells.Add(ShutterListCell);
            if (GrowthList.Instance.GetTargetAction(name) == "heater")
                Row.Cells.Add(HeaterListCell);

            Row.Cells.Add(new DataGridViewTextBoxCell());
            Row.Cells.Add(new DataGridViewTextBoxCell());
            Row.Cells.Add(new DataGridViewTextBoxCell());
            Row.Cells.Add(new DataGridViewTextBoxCell());
        }

        

        private static List<TableColumn> _columnHeaders;

        public static List<TableColumn> ColumnHeaders
        {
            get
            {
                if (_columnHeaders == null)
                {
                    _columnHeaders = new List<TableColumn>()
            {
                new("Действие", Actions.Names),
                new("Номер", CellType._enum),
                new("Задание", CellType._float),
                new("Скорость/Время", CellType._float),
                new("Время", CellType._float),
                new("Комментарий", CellType._string)
            };
                }
                return _columnHeaders;
            }
        }


        private DataGridViewComboBoxCell ActionCell
        {
            get
            {
                var column = new TableColumn("Действие", Actions.Names);

                DataGridViewComboBoxCell viewComboBoxCell = new();
                viewComboBoxCell.MaxDropDownItems = column.EnumType.EnumCount;

                for (int ittr_num = 0; ittr_num < column.EnumType.EnumCount; ++ittr_num)
                    viewComboBoxCell.Items.Add((object)column.EnumType.GetValueByIndex(ittr_num));
                return viewComboBoxCell;
            }
        }

        private static DataGridViewComboBoxCell ShutterListCell
        {
            get
            {
                int ListItemsCount = GrowthList.Instance.ShutterNames.EnumCount;

                DataGridViewComboBoxCell viewComboBoxCell = new();

                for (int i = 0; i < ListItemsCount; i++)
                    viewComboBoxCell.Items.Add(GrowthList.Instance.ShutterNames.GetValueByIndex(i));

                return viewComboBoxCell;
            }
        }

        private static DataGridViewComboBoxCell HeaterListCell
        {
            get
            {
                int ListItemsCount = GrowthList.Instance.HeaterNames.EnumCount;

                DataGridViewComboBoxCell viewComboBoxCell = new();

                for (int i = 0; i < ListItemsCount; i++)
                    viewComboBoxCell.Items.Add(GrowthList.Instance.HeaterNames.GetValueByIndex(i));

                return viewComboBoxCell;
            }
        }

        public bool ChangeNumber(string number)
        {
            try
            {
                _cells[Params.NumberIndex].ParseValue(number);
                Row.Cells[Params.NumberIndex].Value = number;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool ChangeSetpoint(float value)
        {
            if (value >= MinSetpoint && value <= MaxSetpoint)
            {
                _cells[Params.SetpointIndex].ParseValue(value);
                Row.Cells[Params.SetpointIndex].Value = value;
                return true;
            }
            return false;
        }
        public bool ChangeSpeed(float value)
        {
            if (value >= MinTimeSetpoint && value <= MaxTimeSetpoint)
            {
                _cells[Params.TimeSetpointIndex].ParseValue(value);
                Row.Cells[Params.TimeSetpointIndex].Value = value;
                return true;
            }
            return false;
        }


        public float CycleTime
        {
            get => (float)_cells[Params.RecipeTimeIndex].FloatValue;

            set
            {
                _cells[Params.RecipeTimeIndex].ParseValue(value);
                _cells[Params.RecipeTimeIndex].ParseValue(TimeSpan.FromSeconds(value).ToString(@"hh\:mm\:ss\.ff"));
                Row.Cells[Params.RecipeTimeIndex].Value = value;
            }
        }

        public void ChangeComment(string comment)
        {
            _cells[Params.CommentIndex].ParseValue(comment);
            Row.Cells[Params.CommentIndex].Value = comment;
        }

        public int GetNumber()
        {
            return _cells[Params.NumberIndex].IntValue;
        }

        public float GetSetpoint()
        {
            return (float)_cells[Params.SetpointIndex].FloatValue;
        }

        public float GetTime()
        {
            return (float)_cells[Params.TimeSetpointIndex].FloatValue;
        }
    }
}