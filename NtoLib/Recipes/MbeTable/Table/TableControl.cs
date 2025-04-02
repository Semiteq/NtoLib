using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FB;
using FB.VisualFB;
using InSAT.OPC;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.PLC;
using NtoLib.Recipes.MbeTable.RecipeLines;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable
{
    [ComVisible(true)]
    [DisplayName("Таблица рецептов МБЕ")]
    [Guid("8161DF32-8D80-4B81-AF52-3021AE0AD293")]
    public partial class TableControl : VisualControlBase
    {
        #region Private fields

        private const int _rowHeadersWidth = 75;

        private const int ROW_HEIGHT = 32;

        private DataGridView dataGridView1;

        private Button button_add_after;
        private Button button_add_before;
        private Button button_del;
        private Button button_save;
        private Button button_open;

        private TableMode _tableType = TableMode.Edit;
        private Font _headerFont = new("Arial", 16f, FontStyle.Bold);
        private bool _headerFontChanged = true;
        private Color _controlBackgroundColor = Color.White;
        private Color _tableBackgroundColor = Color.White;
        private Color _headerTextColor = Color.Black;
        private bool _headerTextColorChanged = true;
        private Color _header_bg_color = Color.DarkGray;
        private bool _header_bg_color_changed = true;

        private Font _line_font = new("Arial", 14f);
        private Font _selected_line_font = new("Arial", 14f);
        private Font _passed_line_font = new("Arial", 14f);

        private Color _line_text_color = Color.Black;
        private Color _selected_line_text_color = Color.Black;
        private Color _passed_line_text_color = Color.DarkGray;
        private Color _line_bg_color = Color.White;
        private Color _selected_line_bg_color = Color.Green;
        private Color _passed_line_bg_color = Color.Yellow;
        private int _buttons_size = 60;
        private Color _buttons_color;
        private bool _resize = true;
        private int currentRecipeLine = 2;
        private int prevCurrentRecipeLine = -2;
        private bool _to_runtime;

        private List<TableColumn> columns = new();

        private readonly string make_table_msg = "_ ";
        private int make_upload;

        private List<RecipeLine> _tableData = new();

        private readonly StatusManager statusManager;
        private readonly PlcCommunication plcCommunication;
        private readonly RecipeFileReader recipeFileReader;
        private readonly RecipeFileWriter recipeFileWriter;

        private readonly OpenFileDialog openFileDialog1;
        private readonly SaveFileDialog saveFileDialog1;

        #endregion

        #region Properties

        [DisplayName("Режим")]
        public TableMode table_type
        {
            get => _tableType;
            set
            {
                _tableType = value;
                if (_tableType == TableMode.View)
                    ChangeToViewMode();
                else
                    ChangeToEditMode();
            }
        }

        [DisplayName("Цвет фона")]
        public Color control_bg_color
        {
            get => _controlBackgroundColor;
            set
            {
                if (value != Color.Transparent)
                    _controlBackgroundColor = value;
                ((Control)this).BackColor = _controlBackgroundColor;
                DbgMsg.BackColor = _controlBackgroundColor;
            }
        }

        [DisplayName("Цвет фона таблицы")]
        public Color table_bg_color
        {
            get => _tableBackgroundColor;
            set
            {
                if (value != Color.Transparent)
                    _tableBackgroundColor = value;
                dataGridView1.BackgroundColor = _tableBackgroundColor;
            }
        }

        [DisplayName("Шрифт заголовка таблицы")]
        public Font header_font
        {
            get => _headerFont;
            set
            {
                _headerFont = value;
                _headerFontChanged = true;
            }
        }

        [DisplayName("Цвет текста заголовка таблицы")]
        public Color header_text_color
        {
            get => _headerTextColor;
            set
            {
                if (value != Color.Transparent)
                    _headerTextColor = value;
                _headerTextColorChanged = true;
            }
        }

        [DisplayName("Цвет фона заголовка таблицы")]
        public Color header_bg_color
        {
            get => _header_bg_color;
            set
            {
                if (value != Color.Transparent)
                    _header_bg_color = value;
                _header_bg_color_changed = true;
            }
        }

        [DisplayName("Шрифт строки таблицы")]
        public Font line_font
        {
            get => _line_font;
            set
            {
                _line_font = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Цвет текста строки таблицы")]
        public Color line_text_color
        {
            get => _line_text_color;
            set
            {
                if (value != Color.Transparent)
                    _line_text_color = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Цвет фона строки таблицы")]
        public Color line_bg_color
        {
            get => _line_bg_color;
            set
            {
                if (value != Color.Transparent)
                    _line_bg_color = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Шрифт текущей строки таблицы")]
        public Font selected_line_font
        {
            get => _selected_line_font;
            set
            {
                _selected_line_font = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Цвет текста текущей строки таблицы")]
        public Color selected_line_text_color
        {
            get => _selected_line_text_color;
            set
            {
                if (value != Color.Transparent)
                    _selected_line_text_color = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Цвет фона текущей строки таблицы")]
        public Color selected_line_bg_color
        {
            get => _selected_line_bg_color;
            set
            {
                if (value != Color.Transparent)
                    _selected_line_bg_color = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Шрифт пройденной строки таблицы")]
        public Font passed_line_font
        {
            get => _passed_line_font;
            set
            {
                _passed_line_font = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Цвет текста пройденной строки таблицы")]
        public Color passed_line_text_color
        {
            get => _passed_line_text_color;
            set
            {
                if (value != Color.Transparent)
                    _passed_line_text_color = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Цвет фона пройденной строки таблицы")]
        public Color passed_line_bg_color
        {
            get => _passed_line_bg_color;
            set
            {
                if (value != Color.Transparent)
                    _passed_line_bg_color = value;
                ChangeRowFont();
            }
        }

        [DisplayName("Размер кнопок")]
        public int buttons_size
        {
            get => _buttons_size;
            set
            {
                _buttons_size = value;
                _resize = true;
            }
        }

        [DisplayName("Цвет кнопок")]
        public Color buttons_color
        {
            get => _buttons_color;
            set
            {
                if (value != Color.Transparent)
                    _buttons_color = value;
                button_open.BackColor = _buttons_color;
                button_save.BackColor = _buttons_color;
            }
        }

        #endregion

        #region Constructor

        public TableControl() : base(true)
        {
            InitializeComponent();
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dataGridView1.RowHeadersWidth = 90;

            statusManager = new(DbgMsg);

            openFileDialog1 = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                AddExtension = true,
                Multiselect = false,
                Title = "Выберите файл рецепта",
                RestoreDirectory = true
            };

            saveFileDialog1 = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                AddExtension = true,
                Title = "Сохраните файл рецепта",
                RestoreDirectory = true
            };

            plcCommunication = new();
            recipeFileReader = new();
            recipeFileWriter = new();
        }

        #endregion

        #region TableView

        /// <summary>
        /// Table formating for execution mode. Lines before current line are yellow, 
        /// current line is green, after current line is white.
        /// </summary>
        private void ChangeRowFont()
        {
            if (dataGridView1.Rows.Count == 0)
                return;

            for (var i = 0; i < dataGridView1.Rows.Count; i++)
            {
                var rowStyle = dataGridView1.Rows[i].DefaultCellStyle;

                if (i < currentRecipeLine)
                {
                    rowStyle.BackColor = _passed_line_bg_color;
                    rowStyle.Font = _passed_line_font;
                    rowStyle.ForeColor = _passed_line_text_color;

                    // Apply style to each cell in the row to ensure color change
                    foreach (DataGridViewCell cell in dataGridView1.Rows[i].Cells)
                    {
                        cell.Style.BackColor = _passed_line_bg_color;
                    }
                }
                else if (i == currentRecipeLine)
                {
                    rowStyle.BackColor = _selected_line_bg_color;
                    rowStyle.Font = _selected_line_font;
                    rowStyle.ForeColor = _selected_line_text_color;

                    // Apply style to each cell in the row to ensure color change
                    foreach (DataGridViewCell cell in dataGridView1.Rows[i].Cells)
                    {
                        cell.Style.BackColor = _selected_line_bg_color;
                    }
                }
                else
                {
                    rowStyle.BackColor = _line_bg_color;
                    rowStyle.Font = _line_font;
                    rowStyle.ForeColor = _line_text_color;

                    // Apply style to each cell in the row to ensure color change
                    foreach (DataGridViewCell cell in dataGridView1.Rows[i].Cells)
                    {
                        cell.Style.BackColor = _line_bg_color;
                    }
                }
            }

            dataGridView1.Refresh();
        }

        /// <summary>
        /// Конфигурирует ширину, тип и названия столбцов.
        /// </summary>
        private void ConfigureColumns(bool editMode)
        {
            statusManager.WriteStatusMessage("Подготовка таблицы. ");
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            columns = RecipeLine.ColumnHeaders;
            foreach (TableColumn column in columns)
            {
                if (editMode)
                {
                    if (column.Type == CellType.Bool)
                    {
                        DataGridViewComboBoxColumn viewComboBoxColumn = new()
                        {
                            SortMode = DataGridViewColumnSortMode.NotSortable,
                            Name = column.Name,
                            Tag = column,
                            Width = GetWidth(column),
                            MaxDropDownItems = 2
                        };
                        viewComboBoxColumn.Items.AddRange("Да", "Нет");

                        column.GridIndex = dataGridView1.Columns.Add(viewComboBoxColumn);
                    }
                    else if (column.Type == CellType.Enum)
                    {
                        DataGridViewComboBoxColumn viewComboBoxColumn = new()
                        {
                            SortMode = DataGridViewColumnSortMode.NotSortable,
                            Name = column.Name,
                            Tag = column,
                            Width = GetWidth(column)
                        };

                        if (column.IntStringMap != null && column.GridIndex == Params.ActionIndex)
                        {
                            foreach (var item in column.IntStringMap.Values)
                                viewComboBoxColumn.Items.Add(item);
                        }

                        column.GridIndex = dataGridView1.Columns.Add(viewComboBoxColumn);
                    }
                    else
                    {
                        DataGridViewTextBoxColumn viewTextBoxColumn = new()
                        {
                            SortMode = DataGridViewColumnSortMode.NotSortable,
                            Name = column.Name,
                            Tag = column,
                            Width = GetWidth(column)
                        };

                        column.GridIndex = dataGridView1.Columns.Add(viewTextBoxColumn);
                    }
                }
                else
                {
                    DataGridViewTextBoxColumn viewTextBoxColumn = new()
                    {
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                        Name = column.Name,
                        Tag = column,
                        Width = GetWidth(column)
                    };

                    column.GridIndex = dataGridView1.Columns.Add(viewTextBoxColumn);
                }
            }

            ChangeCellAlignment();

            statusManager.WriteStatusMessage("Таблица подготовлена. ");
        }

        /// <summary>
        /// Возвращает ширину столбца в зависимости от имени. 
        /// Код в этом методе нарушает положения Женевской конвенции, его обязательно надо будет переделать
        /// </summary>
        private int GetWidth(TableColumn column)
        {
            return column.Name switch
            {
                "Действие" => 200,
                "Объект" => 150,
                "Задание" => 180,
                "Нач.значение" => 200,
                "Скорость" => 150,
                "Длительность" => 200,
                "Время" => 150,
                _ => dataGridView1.Width - (200 + 150 + 180 + 200 + 150 + 200 + 150) - _rowHeadersWidth - 20
            };
        }

        private void ChangeCellAlignment()
        {
            for (int cellIndex = 1; cellIndex < Params.ColumnCount; cellIndex++)
            {
                dataGridView1.Columns[cellIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns[cellIndex].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }


        private void BlockCells(int rowIndex)
        {
            for (int cellIndex = 1; cellIndex < Params.ColumnCount - 1; cellIndex++)
            {
                if (_tableData[rowIndex].Cells[cellIndex].Type == CellType.Blocked)
                {
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].ReadOnly = true;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Style.BackColor = Color.LightGray;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Value = "";
                }
                else
                {
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].ReadOnly = false;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Style.BackColor = Color.White;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Value =
                        _tableData[rowIndex].Cells[cellIndex].GetValue();
                }
            }
        }

        #endregion

        #region OnPaint

        protected override void OnPaint(PaintEventArgs e)
        {
            UpdateFlags();
            UpdateHeaderStyles();
            UpdateRowFont();

            if (FBConnector?.DesignMode == true)
                return;

            UpdateGrowthList();
            UpdatePinValues();
        }

        private void UpdateFlags()
        {
            _resize = false;
            if (_to_runtime)
            {
                currentRecipeLine = -1;
                prevCurrentRecipeLine = -2;
                _to_runtime = false;
            }
        }

        private void UpdateHeaderStyles()
        {
            if (!_headerFontChanged && !_headerTextColorChanged && !_header_bg_color_changed)
                return;

            var rowStyle = dataGridView1.RowHeadersDefaultCellStyle;
            var colStyle = dataGridView1.ColumnHeadersDefaultCellStyle;

            if (_headerFontChanged)
            {
                rowStyle.Font = _headerFont;
                colStyle.Font = _headerFont;
                _headerFontChanged = false;
            }

            if (_headerTextColorChanged)
            {
                rowStyle.ForeColor = _headerTextColor;
                colStyle.ForeColor = _headerTextColor;
                _headerTextColorChanged = false;
            }

            if (_header_bg_color_changed)
            {
                rowStyle.BackColor = _header_bg_color;
                colStyle.BackColor = _header_bg_color;
                _header_bg_color_changed = false;
            }
        }

        private void UpdateRowFont()
        {
            if (currentRecipeLine == prevCurrentRecipeLine)
                return;

            ChangeRowFont();
            prevCurrentRecipeLine = currentRecipeLine;
        }

        private void UpdateGrowthList()
        {
            var shutterPins = new ReadPins(Params.IdFirstShutterName, Params.ShutterNameQuantity,
                FBConnector.Fb as MbeTableFB);
            if (shutterPins.IsPinGroupQualityGood())
                ActionTarget.SetNames(ActionType.Shutter, shutterPins.ReadPinNames());

            var heaterPins = new ReadPins(Params.IdFirstHeaterName, Params.HeaterNameQuantity,
                FBConnector.Fb as MbeTableFB);
            if (heaterPins.IsPinGroupQualityGood())
                ActionTarget.SetNames(ActionType.Heater, heaterPins.ReadPinNames());

            var nitrogenSource = new ReadPins(Params.IdFirstNitrogenSourceName, Params.NitrogenSourceNameQuantity,
                FBConnector.Fb as MbeTableFB);
            if (nitrogenSource.IsPinGroupQualityGood())
                ActionTarget.SetNames(ActionType.NitrogenSource, nitrogenSource.ReadPinNames());
        }

        private void UpdatePinValues()
        {
            var fb = (FBBase)FBConnector;
            uint pinValue1 = fb.GetPinValue<uint>(Params.IdHmiStatus);
            OpcQuality pinQuality1 = fb.GetPinQuality(Params.IdHmiStatus);
            int pinValue2 = fb.GetPinValue<int>(Params.IdHmiActualLine);
            OpcQuality pinQuality2 = fb.GetPinQuality(Params.IdHmiActualLine);

            button_save.Visible = true;

            if (pinQuality2 != OpcQuality.Good || pinQuality1 != OpcQuality.Good || (pinValue1 & 4) != 4)
                return;

            currentRecipeLine = pinValue2;
            UpdateRowFont();
        }

        private void ChangeToViewMode()
        {
            button_open.Enabled = true;
        }

        private void ChangeToEditMode()
        {
            button_open.Enabled = true;
            button_save.Visible = true;
            button_save.Enabled = true;
            button_del.Visible = true;
            button_add_after.Visible = true;
            button_add_before.Visible = true;
        }

        #endregion

        #region MasterScada methods override

        protected override void ToDesign()
        {
            ConfigureColumns(false);
            currentRecipeLine = 2;
            ChangeRowFont();
            statusManager.WriteStatusMessage(make_table_msg, false);
            make_upload = 0;
        }

        protected override void ToRuntime()
        {
            _to_runtime = true;
            DbgMsg.Text = "";
            ConfigureColumns(_tableType == TableMode.Edit);
            make_upload = 1;
            dataGridView1.ReadOnly = _tableType == TableMode.View;
            _tableData.Clear();
        }

        #endregion

        private void UpdateEnumDropDown(DataGridViewComboBoxCell cell, object cellValue)
        {
            try
            {
                cell.Items.Clear();

                // Fill combobox depending on the action type
                switch (ActionManager.GetTargetAction(cellValue.ToString()))
                {
                    case ActionType.Shutter:
                        var shutterNames = ActionTarget.ShutterNames.Values;
                        cell.MaxDropDownItems = shutterNames.Count;
                        foreach (var name in shutterNames)
                        {
                            cell.Items.Add(name);
                        }

                        break;

                    case ActionType.Heater:
                        var heaterNames = ActionTarget.HeaterNames.Values;
                        cell.MaxDropDownItems = heaterNames.Count;
                        foreach (var name in heaterNames)
                        {
                            cell.Items.Add(name);
                        }

                        break;

                    case ActionType.NitrogenSource:
                        var nitrogenSourceNames = ActionTarget.NitrogenSourceNames.Values;
                        cell.MaxDropDownItems = nitrogenSourceNames.Count;
                        foreach (var name in nitrogenSourceNames)
                        {
                            cell.Items.Add(name);
                        }

                        break;

                    case ActionType.Unspecified:
                        cell.MaxDropDownItems = 1;
                        cell.Items.Add(cellValue);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                cell.Value = cell.Items.Contains(cellValue) ? cellValue : cell.Items.Count > 0 ? cell.Items[0] : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении комбобокса: {ex.Message}");
            }
        }

        #region CellChange

        private void EndCellEdit(object sender, DataGridViewCellEventArgs e)
        {
            var rowIndex = e.RowIndex;
            var columnIndex = e.ColumnIndex;
            var currentCell = dataGridView1.Rows[rowIndex].Cells[columnIndex];

            switch (columnIndex)
            {
                case Params.ActionTargetIndex:
                    HandleActionTargetEdit(rowIndex, currentCell);
                    break;
                case Params.SetpointIndex:
                case Params.InitialValueIndex:
                case Params.SpeedIndex:
                case Params.TimeSetpointIndex:
                    HandleNumericCellEdit(rowIndex, columnIndex);
                    break;
                case Params.CommentIndex:
                    _tableData[rowIndex].Comment = currentCell.Value.ToString();
                    break;
            }
        }

        private void HandleNumericCellEdit(int rowIndex, int columnIndex)
        {
            var currentCell = dataGridView1.Rows[rowIndex].Cells[columnIndex];
            if (TryParseCellValue(currentCell.Value?.ToString(), out var value) &&
                ValidateCellValue(rowIndex, columnIndex, value))
            {
                _tableData[rowIndex].Cells[columnIndex].ParseValue(value);
                if (columnIndex == Params.TimeSetpointIndex)
                    ProcessSpeed(rowIndex);
                else
                    ProcessTime(rowIndex);
                currentCell.Value = _tableData[rowIndex].Cells[columnIndex].GetValue();
                RefreshTable();
            }
            else
            {
                currentCell.Value = _tableData[rowIndex].Cells[columnIndex].GetValue();

                var isTimeColumn = columnIndex == Params.TimeSetpointIndex;
                ShowError("Введите корректное значение",
                    _tableData[rowIndex].GetMinValue(columnIndex).ToString(),
                    _tableData[rowIndex].GetMaxValue(columnIndex).ToString(),
                    _tableData[rowIndex].Cells[columnIndex].GetUnits());
            }
        }

        private bool TryParseCellValue(string input, out float value)
        {
            value = 0;
            if (string.IsNullOrEmpty(input)) return false;

            var trimmedInput = input.Trim();

            // Try parse time format hh:mm:ss[.ms]
            if (trimmedInput.Contains(':'))
            {
                var parts = trimmedInput.Split(':');
                if (parts.Length >= 2)
                {
                    var hours = int.TryParse(parts[0], out var h) ? h : 0;
                    var minutes = int.TryParse(parts[1], out var m) ? m : 0;
                    var seconds = parts.Length > 2 && int.TryParse(parts[2].Split('.')[0], out var s) ? s : 0;
                    var milliseconds = parts.Length > 2 && parts[2].Contains('.') &&
                                       float.TryParse(("0" + parts[2].Split('.')[1]).Replace('.', ','), out var ms)
                        ? ms
                        : 0;

                    value = hours * 3600 + minutes * 60 + seconds + milliseconds / 1000;
                    return true;
                }
            }

            // Try parse regular numeric value
            var numericPart =
                new string(trimmedInput.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            numericPart = numericPart.Replace('.', ',');
            return float.TryParse(numericPart, out value);
        }

        private bool ValidateCellValue(int rowIndex, int columnIndex, float value)
        {
            return columnIndex switch
            {
                Params.SetpointIndex => _tableData[rowIndex].ValidateSetpoint(value),
                Params.InitialValueIndex => _tableData[rowIndex].ValidateInitialValue(value),
                Params.SpeedIndex => _tableData[rowIndex].ValidateSpeed(value),
                Params.TimeSetpointIndex => _tableData[rowIndex].ValidateTimeSetpoint(value),
                _ => false
            };
        }

        private void HandleActionTargetEdit(int rowIndex, DataGridViewCell currentCell)
        {
            _tableData[rowIndex].TargetAction = currentCell.Value?.ToString() ?? "";
            RefreshTable();
        }

        private void ProcessTime(int rowIndex)
        {
            var currentCommand = _tableData[rowIndex].Cells[Params.ActionIndex].StringValue;
            if (currentCommand == "TEMP + WAIT" || currentCommand == "POWER + WAIT") return;

            var setpoint = _tableData[rowIndex].Cells[Params.SetpointIndex].FloatValue;
            var initial = _tableData[rowIndex].Cells[Params.InitialValueIndex].FloatValue;
            var speed = _tableData[rowIndex].Cells[Params.SpeedIndex].FloatValue;

            if (Math.Abs(setpoint - initial) * 60 / speed != float.NaN)
            {
                _tableData[rowIndex].Duration = Math.Abs(setpoint - initial) * 60 / speed;
                dataGridView1.Rows[rowIndex].Cells[Params.TimeSetpointIndex].Value =
                    _tableData[rowIndex].Cells[Params.TimeSetpointIndex].GetValue();
            }
            else
            {
                statusManager.WriteStatusMessage("Скорость не может быть равна 0", true);
            }
        }

        private void ProcessSpeed(int rowIndex)
        {
            var currentCommand = _tableData[rowIndex].Cells[Params.ActionIndex].StringValue;
            if (currentCommand == "TEMP + WAIT" || currentCommand == "POWER + WAIT") return;

            var setpoint = _tableData[rowIndex].Cells[Params.SetpointIndex].FloatValue;
            var initial = _tableData[rowIndex].Cells[Params.InitialValueIndex].FloatValue;
            var time = _tableData[rowIndex].Cells[Params.TimeSetpointIndex].FloatValue;

            if (Math.Abs(setpoint - initial) * 60 / time != float.NaN)
            {
                _tableData[rowIndex].Speed = Math.Abs(setpoint - initial) * 60 / time;
                dataGridView1.Rows[rowIndex].Cells[Params.SpeedIndex].Value =
                    _tableData[rowIndex].Cells[Params.SpeedIndex].GetValue();
            }
            else
            {
                statusManager.WriteStatusMessage("Время не может быть равно 0", true);
            }
        }

        private void ShowError(string message, string min, string max, string unit = "")
        {
            MessageBox.Show(
                $@"{message}:
                минимальное значение: {min}{unit}
                максимальное значение: {max}{unit}",
                @"Ошибка ввода данных",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var columnIndex = e.ColumnIndex;
            var rowIndex = e.RowIndex;

            var comboBox = (DataGridViewComboBoxCell)dataGridView1.Rows[rowIndex].Cells[Params.ActionIndex];

            if (comboBox.Value != null && columnIndex == Params.ActionIndex && !isLoadingActive)
            {
                dataGridView1.Rows[rowIndex].HeaderCell.Value = (rowIndex + 1).ToString();

                var command = (string)dataGridView1.Rows[rowIndex].Cells[columnIndex].Value;
                var minNumber = ActionTarget.GetMinNumber(command);

                ReplaceLineInRecipe(RecipeLineFactory.NewLine(command, minNumber, -1f, -1f, 0f, 0f, ""));

                RefreshTable();
                dataGridView1.Invalidate();
            }
        }

        #endregion

        /// <summary> Таймер для автоматической подгрузки рецепта из ПЛК </summary>
        private Timer _loadDelay;

        private void HandleVisibleChanged(object sender, EventArgs e)
        {
            if (!Visible)
                return;

            if (!DesignMode && _tableType == TableMode.View)
            {
                if (_loadDelay == null)
                {
                    _loadDelay = new Timer();
                    _loadDelay.Interval = 1000;

                    _loadDelay.Tick += (object sender2, EventArgs e2) =>
                    {
                        if (TryLoadRecipeFromPlc())
                            statusManager.WriteStatusMessage("Рецепт загружен из ПЛК");

                        _loadDelay.Stop();
                    };
                }

                _loadDelay.Start();
            }
        }
    }
}