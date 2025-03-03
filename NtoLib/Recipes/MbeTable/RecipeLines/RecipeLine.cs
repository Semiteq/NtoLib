using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal abstract class RecipeLine
    {
        public List<TCell> GetCells => Cells;

        protected List<TCell> Cells = new();

        // Number of nested cycle todo: убрать в TableLoops
        public int TabulateLevel = 0;

        public int MinNumber { get; private set; } = 1;
        public int MaxNumber { get; private set; } = 16;

        public float MinSetpoint { get; protected set; }
        public float MaxSetpoint { get; protected set; }

        public float MinTimeSetpoint { get; protected set; } = 0.1f;
        public float MaxTimeSetpoint { get; protected set; } = 10800;
        public float MinSpeed { get; protected set; } = 0.1f;
        public float MaxSpeed { get; protected set; } = 1000;

        public abstract ActionTime ActionTime { get; }

        public readonly DataGridViewRow Row;

        protected string ShutterName;
        protected string HeaterName;
        protected string NitrogenSourceName;

        protected RecipeLine(string name)
        {
            // Filling new line from left to right
            Row = new DataGridViewRow();

            Row.Cells.Add(ActionCell);
            Row.Cells[Params.ActionIndex].Value = name;

            switch (ActionManager.GetTargetAction(name))
            {
                case ActionType.Shutter:
                    Row.Cells.Add(ShutterListCell);
                    break;
                case ActionType.Heater:
                    Row.Cells.Add(HeaterListCell);
                    break;
                case ActionType.NitrogenSource:
                    Row.Cells.Add(NitrogenSourceListCell);
                    break;
            }

            Row.Cells.Add(new DataGridViewTextBoxCell());
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
                    new("Объект", CellType._enum),
                    new("Задание", CellType._float),
                    new("Нач.значение", CellType._float),
                    new("Скорость", CellType._float),
                    new("Длительность", CellType._float),
                    new("Время", CellType._float),
                    new("Комментарий", CellType._string)
                };
            }
        }


        private DataGridViewComboBoxCell ActionCell
        {
            get
            {
                DataGridViewComboBoxCell viewComboBoxCell = new()
                {
                    MaxDropDownItems = ActionManager.Names.Count()
                };
                foreach (var action in ActionManager.Names.Values)
                    viewComboBoxCell.Items.Add(action);
                return viewComboBoxCell;
            }
        }

        private static DataGridViewComboBoxCell ShutterListCell
        {
            get
            {
                DataGridViewComboBoxCell viewComboBoxCell = new();
                foreach (var shutter in GrowthList.ShutterNames.Values)
                    viewComboBoxCell.Items.Add(shutter);
                return viewComboBoxCell;
            }
        }

        private static DataGridViewComboBoxCell HeaterListCell
        {
            get
            {
                DataGridViewComboBoxCell viewComboBoxCell = new();
                foreach (var heater in GrowthList.HeaterNames.Values)
                    viewComboBoxCell.Items.Add(heater);
                return viewComboBoxCell;
            }
        }

        private static DataGridViewComboBoxCell NitrogenSourceListCell
        {
            get
            {
                DataGridViewComboBoxCell viewComboBoxCell = new();
                foreach (var source in GrowthList.NitrogenSourceNames.Values)
                    viewComboBoxCell.Items.Add(source);
                return viewComboBoxCell;
            }
        }

        public bool ChangeTargetAction(string action)
        {
            try
            {
                Cells[Params.ActionTargetIndex].ParseValue(action);
                Row.Cells[Params.ActionTargetIndex].Value = action;
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
                Cells[Params.SetpointIndex].ParseValue(value);
                Row.Cells[Params.SetpointIndex].Value = value;
                return true;
            }
            return false;
        }

        public bool ChangeInitialValue(float value)
        {
            if (value >= MinSetpoint && value <= MaxSetpoint)
            {
                Cells[Params.InitialValueIndex].ParseValue(value);
                Row.Cells[Params.InitialValueIndex].Value = value;
                return true;
            }
            return false;
        }

        public bool ChangeSpeed(float value)
        {
            if (value >= MinSpeed && value <= MaxSpeed)
            {
                Cells[Params.SpeedIndex].ParseValue(value);
                Row.Cells[Params.SpeedIndex].Value = value;
                return true;
            }
            return false;
        }

        public bool ChangeTime(float value)
        {
            if (value >= MinTimeSetpoint && value <= MaxTimeSetpoint)
            {
                Cells[Params.TimeSetpointIndex].ParseValue(value);
                Row.Cells[Params.TimeSetpointIndex].Value = value;
                return true;
            }
            return false;
        }

        public float CycleTime
        {
            get => (float)Cells[Params.RecipeTimeIndex].FloatValue;

            set
            {
                Cells[Params.RecipeTimeIndex].ParseValue(value);
                Cells[Params.RecipeTimeIndex].ParseValue(TimeSpan.FromSeconds(value).ToString(@"hh\:mm\:ss\.ff"));
                Row.Cells[Params.RecipeTimeIndex].Value = value;
            }
        }

        public void ChangeComment(string comment)
        {
            Cells[Params.CommentIndex].ParseValue(comment);
            Row.Cells[Params.CommentIndex].Value = comment;
        }

        public int GetNumber()
        {
            return Cells[Params.ActionIndex].IntValue;
        }

        public float GetSetpoint()
        {
            return (float)Cells[Params.SetpointIndex].FloatValue;
        }

        public float GetInitialValue()
        {
            return (float)Cells[Params.InitialValueIndex].FloatValue;
        }
        public float GetTime()
        {
            return (float)Cells[Params.TimeSetpointIndex].FloatValue;
        }
        public float GetSpeed()
        {
            return (float)Cells[Params.SpeedIndex].FloatValue;
        }
    }
}