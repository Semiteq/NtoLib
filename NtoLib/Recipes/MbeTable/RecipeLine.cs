using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{
    internal abstract class RecipeLine
    {
        public List<TCell> GetCells => this._cells;

        public const int CommandIndex = 0;
        public const int NumberIndex = 1;
        public const int SetpointIndex = 2;
        public const int TimeSetpointIndex = 3;
        public const int RecipeTimeIndex = 4;
        public const int CommentIndex = 5;

        public const int ColumnCount = 6;

        protected List<TCell> _cells = new List<TCell>();

        public int tabulateLevel = 0;

        public int MinNumber { get; private set; } = 1;
        public int MaxNumber { get; private set; } = 16;

        public float MinSetpoint { get; protected set; }
        public float MaxSetpoint { get; protected set; }

        public float MinTimeSetpoint { get; protected set; } = 0.1f;
        public float MaxTimeSetpoint { get; protected set; } = 10800;

        public abstract ActionTime ActionTime { get; }

        public DataGridViewRow Row;

        protected RecipeLine(string name)
        {
            Row = new DataGridViewRow();
            DataGridViewCellCollection dataGridViewCellCollection = new DataGridViewCellCollection(Row);

            Row.Cells.Add(ActionCell);
            Row.Cells.Add(new DataGridViewTextBoxCell());
            Row.Cells.Add(new DataGridViewTextBoxCell());
            Row.Cells.Add(new DataGridViewTextBoxCell());
            Row.Cells.Add(new DataGridViewTextBoxCell());
            Row.Cells.Add(new DataGridViewTextBoxCell());

            Row.Cells[0].Value = name;
        }

        public static TableEnumType Actions
        {
            // TODO: переделать! Убрать постоянную реинициализацию!
            get 
            {
                var actions = new TableEnumType("Actions");
                actions.AddEnum(Commands.CLOSE, 10);
                actions.AddEnum(Commands.OPEN, 20);
                actions.AddEnum(Commands.OPEN_TIME, 30);
                actions.AddEnum(Commands.CLOSE_ALL, 40);

                actions.AddEnum(Commands.TEMP, 50);
                actions.AddEnum(Commands.TEMP_WAIT, 60);
                actions.AddEnum(Commands.TEMP_SMOOTH, 70);
                actions.AddEnum(Commands.TEMP_BY_TIME, 80);

                actions.AddEnum(Commands.POWER, 90);
                actions.AddEnum(Commands.POWER_WAIT, 100);
                actions.AddEnum(Commands.POWER_SMOOTH, 110);
                actions.AddEnum(Commands.POWER_BY_TIME, 120);

                actions.AddEnum(Commands.WAIT, 130);

                actions.AddEnum(Commands.FOR, 140);
                actions.AddEnum(Commands.END_FOR, 150);
                actions.AddEnum(Commands.PAUSE, 160);

                actions.AddEnum(Commands.NH3_OPEN, 170);
                actions.AddEnum(Commands.NH3_CLOSE, 180);
                actions.AddEnum(Commands.NH3_PURGE, 190);

                return actions;
            }
            private set { }
        }

        public static List<TableColumn> ColumnHeaders
        {
            // TODO: переделать! Убрать постоянную реинициализацию!
            get
            {
                return new List<TableColumn>() 
                {
                    new TableColumn("Действие", Actions),
                    new TableColumn("Номер", CellType._int),
                    new TableColumn("Задание", CellType._float),
                    new TableColumn("Скорость/Время", CellType._float),
                    new TableColumn("Время", CellType._float),
                    new TableColumn("Комментарий", CellType._string)
                };
            }
        }

        private DataGridViewComboBoxCell ActionCell
        {
            get
            {
                var column = new TableColumn("Действие", Actions);
                DataGridViewComboBoxCell viewComboBoxCell = new DataGridViewComboBoxCell();
                viewComboBoxCell.MaxDropDownItems = column.EnumType.enum_counts;
                for (int ittr_num = 0; ittr_num < column.EnumType.enum_counts; ++ittr_num)
                    viewComboBoxCell.Items.Add((object)column.EnumType.GetNameByIterrator(ittr_num));
                return viewComboBoxCell;
            }
        }


        public bool ChangeNumber(int number)
        {
            if (number >= MinNumber && number <= MaxNumber)
            {
                _cells[NumberIndex].SetNewValue(number);
                Row.Cells[NumberIndex].Value = number;
                return true;
            }
            return false;
        }
        public bool ChangeSetpoint(float value)
        {
            if(value >= MinSetpoint && value <= MaxSetpoint)
            {
                _cells[SetpointIndex].SetNewValue(value);
                Row.Cells[SetpointIndex].Value = value;
                return true;
            }
            return false;
        }
        public bool ChangeSpeed(float value)
        {
            if (value >= MinTimeSetpoint && value <= MaxTimeSetpoint)
            {
                _cells[TimeSetpointIndex].SetNewValue(value);
                Row.Cells[TimeSetpointIndex].Value = value;
                return true;
            }
            return false;
        }


        public float CycleTime 
        {
            get => (float)_cells[RecipeTimeIndex].FloatValue;
            
            set 
            {
                _cells[RecipeTimeIndex].SetNewValue(value);
                _cells[RecipeTimeIndex].SetNewValue(TimeSpan.FromSeconds(value).ToString(@"hh\:mm\:ss\.ff"));
                Row.Cells[RecipeTimeIndex].Value = value;
            }
        }

        public void ChangeComment(string comment)
        {
            _cells[CommentIndex].SetNewValue(comment);
            Row.Cells[CommentIndex].Value = comment;
        }

        public int GetNumber()
        {
            return _cells[NumberIndex].IntValue;
        }

        public float GetSetpoint()
        {
            return (float)_cells[SetpointIndex].FloatValue;
        }

        public float GetTime()
        {
            return (float)_cells[TimeSetpointIndex].FloatValue;
        }
    }
}