using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal abstract class RecipeLine
    {
        public List<TCell> Cells { get; protected set; } = new();
        public int TabulateLevel { get; set; } = 0;
        public int MinNumber { get; } = 1;
        public int MaxNumber { get; } = 16;
        protected float MinSetpoint { get; set; }
        protected float MaxSetpoint { get; set; }
        protected float MinTimeSetpoint { get; set; } = 0.1f;
        protected float MaxTimeSetpoint { get; set; } = 10800;
        private float MinSpeed { get; set; } = 0.1f;
        private float MaxSpeed { get; set; } = 1000;
        public abstract ActionTime ActionTime { get; }
        public readonly DataGridViewRow Row;

        protected string ShutterName;
        protected string HeaterName;
        protected string NitrogenSourceName;

        protected RecipeLine(string name)
        {
            Row = new DataGridViewRow()
            {
                Cells =
                {
                    ActionListCell(name),
                    TargetActionList(name),
                    new DataGridViewTextBoxCell(),
                    new DataGridViewTextBoxCell(),
                    new DataGridViewTextBoxCell(),
                    new DataGridViewTextBoxCell(),
                    new DataGridViewTextBoxCell()
                }
            };
        }

        private static List<TableColumn> _columnHeaders;

        public static List<TableColumn> ColumnHeaders
        {
            get
            {
                return _columnHeaders ??= new List<TableColumn>()
                {
                    new("Действие", ActionManager.Names),
                    new("Объект", CellType.Enum),
                    new("Задание", CellType.Float),
                    new("Нач.значение", CellType.Float),
                    new("Скорость", CellType.Float),
                    new("Длительность", CellType.Float),
                    new("Время", CellType.Float),
                    new("Комментарий", CellType.String)
                };
            }
        }

        // In this case it is impossible to use link to private static object due to windows forms implementation
        // New lists should be created on each call

        private static DataGridViewComboBoxCell ActionListCell(string name)
        {
            var cell = new DataGridViewComboBoxCell();
            cell.Items.AddRange(ActionManager.Names.Values.ToArray<object>());
            cell.MaxDropDownItems = ActionManager.Names.Count;
            cell.Value = name;
            return cell;
        }

        private static DataGridViewComboBoxCell TargetActionList(string name)
        {
            switch (ActionManager.GetTargetAction(name))
            {
                case ActionType.Shutter:
                    return ShutterListCell();
                case ActionType.Heater:
                    return HeaterListCell();
                case ActionType.NitrogenSource:
                    return NitrogenSourceListCell();
                default:
                    return new DataGridViewComboBoxCell();
            }
        }

        private static DataGridViewComboBoxCell ShutterListCell()
        {
            var cell = new DataGridViewComboBoxCell();
            cell.Items.AddRange(ActionTarget.ShutterNames.Values.ToArray<object>());
            cell.MaxDropDownItems = ActionTarget.ShutterNames.Count;
            return cell;
        }

        private static DataGridViewComboBoxCell HeaterListCell()
        {
            var cell = new DataGridViewComboBoxCell();
            cell.Items.AddRange(ActionTarget.HeaterNames.Values.ToArray<object>());
            cell.MaxDropDownItems = ActionTarget.HeaterNames.Count;
            return cell;
        }

        private static DataGridViewComboBoxCell NitrogenSourceListCell()
        {
            var cell = new DataGridViewComboBoxCell();
            cell.Items.AddRange(ActionTarget.NitrogenSourceNames.Values.ToArray<object>());
            cell.MaxDropDownItems = ActionTarget.NitrogenSourceNames.Count;
            return cell;
        }

        public string TargetAction
        {
            get => Cells[Params.ActionTargetIndex].StringValue;
            set => Cells[Params.ActionTargetIndex].ParseValue(value);
        }

        public float Setpoint
        {
            get => Cells[Params.SetpointIndex].FloatValue;
            set => Cells[Params.SetpointIndex].ParseValue(value);
        }

        public float InitialValue
        {
            get => Cells[Params.InitialValueIndex].FloatValue;
            set => Cells[Params.InitialValueIndex].ParseValue(value);
        }

        public float Speed
        {
            get => Cells[Params.SpeedIndex].FloatValue;
            set => Cells[Params.SpeedIndex].ParseValue(value);
        }

        public float Duration
        {
            get => Cells[Params.TimeSetpointIndex].FloatValue;
            set => Cells[Params.TimeSetpointIndex].ParseValue(value);
        }

        public float Time
        {
            get => Cells[Params.RecipeTimeIndex].FloatValue;
            set => Cells[Params.RecipeTimeIndex].ParseValue(value);
        }

        public string Comment
        {
            get => Cells[Params.CommentIndex].StringValue;
            set => Cells[Params.CommentIndex].ParseValue(value);
        }

        public bool ValidateSetpoint(float value) => value >= MinSetpoint && value <= MaxSetpoint;

        public bool ValidateInitialValue(float value) => value >= MinSetpoint && value <= MaxSetpoint;
        public bool ValidateTimeSetpoint(float value) => value >= MinTimeSetpoint && value <= MaxTimeSetpoint;
        public bool ValidateSpeed(float value) => value >= MinSpeed && value <= MaxSpeed;

        public float GetMinValue(int columnIndex) => columnIndex switch
        {
            Params.SetpointIndex => MinSetpoint,
            Params.InitialValueIndex => MinSetpoint,
            Params.SpeedIndex => MinSpeed,
            Params.TimeSetpointIndex => MinTimeSetpoint,
            _ => float.MinValue
        };

        public float GetMaxValue(int columnIndex) => columnIndex switch
        {
            Params.SetpointIndex => MaxSetpoint,
            Params.InitialValueIndex => MaxSetpoint,
            Params.SpeedIndex => MaxSpeed,
            Params.TimeSetpointIndex => MaxTimeSetpoint,
            _ => float.MaxValue
        };
    }
}