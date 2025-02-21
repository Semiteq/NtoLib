using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable
{
    internal abstract class RecipeLine
    {
        public List<TCell> GetCells => _cells;

        protected List<TCell> _cells = new();

        // Number of nested cycle todo: убрать в TableLoops
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
        protected string nitrogenSourceName;

        protected RecipeLine(string name)
        {
            // Filling new line from left to right
            Row = new DataGridViewRow();

            Row.Cells.Add(ActionCell);
            Row.Cells[Params.CommandIndex].Value = name;

            if (ActionManager.GetTargetAction(name) == ActionType.Shutter)
                Row.Cells.Add(ShutterListCell);
            if (ActionManager.GetTargetAction(name) == ActionType.Heater)
                Row.Cells.Add(HeaterListCell);
            if (ActionManager.GetTargetAction(name) == ActionType.NitrogenSource)
                Row.Cells.Add(NitrogenSourceListCell);

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
                return _columnHeaders ??= new List<TableColumn>()
                {
                    new("Действие", ActionManager.Names),
                    new("Номер", CellType._enum),
                    new("Задание", CellType._float),
                    new("Скорость/Время", CellType._float),
                    new("Время", CellType._float),
                    new("Комментарий", CellType._string)
                };
            }
        }


        private DataGridViewComboBoxCell ActionCell
        {
            get
            {
                var column = new TableColumn("Действие", ActionManager.Names);

                DataGridViewComboBoxCell viewComboBoxCell = new();
                viewComboBoxCell.MaxDropDownItems = column.EnumType.EnumCount;

                for (var ittrNum = 0; ittrNum < column.EnumType.EnumCount; ++ittrNum)
                    viewComboBoxCell.Items.Add((object)column.EnumType.GetValueByIndex(ittrNum));
                return viewComboBoxCell;
            }
        }

        private static DataGridViewComboBoxCell ShutterListCell
        {
            get
            {
                var listItemsCount = GrowthList.ShutterNames.EnumCount;

                DataGridViewComboBoxCell viewComboBoxCell = new();

                for (var i = 0; i < listItemsCount; i++)
                    viewComboBoxCell.Items.Add(GrowthList.ShutterNames.GetValueByIndex(i));

                return viewComboBoxCell;
            }
        }

        private static DataGridViewComboBoxCell HeaterListCell
        {
            get
            {
                var listItemsCount = GrowthList.HeaterNames.EnumCount;

                DataGridViewComboBoxCell viewComboBoxCell = new();

                for (var i = 0; i < listItemsCount; i++)
                    viewComboBoxCell.Items.Add(GrowthList.HeaterNames.GetValueByIndex(i));

                return viewComboBoxCell;
            }
        }
        
        private static DataGridViewComboBoxCell NitrogenSourceListCell
        {
            get
            {
                var listItemsCount = GrowthList.NitrogenSourceNames.EnumCount;

                DataGridViewComboBoxCell viewComboBoxCell = new();

                for (var i = 0; i < listItemsCount; i++)
                    viewComboBoxCell.Items.Add(GrowthList.NitrogenSourceNames.GetValueByIndex(i));

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