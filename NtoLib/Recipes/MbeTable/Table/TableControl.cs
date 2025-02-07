﻿using FB;
using FB.VisualFB;
using InSAT.OPC;
using NtoLib.Devices.Pumps;
using NtoLib.Recipes.MbeTable.Actions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        private const int _max_data_lenght_modbus = 100;

        private DataGridView dataGridView1;

        private Button button_add_after;
        private Button button_add_before;
        private Button button_del;
        private Button button_save;
        private Button button_open;

        private OpenFileDialog openFileDialog1;
        private SaveFileDialog saveFileDialog1;

        private TableMode _tableType = TableMode.Edit;
        private Font _headerFont = new Font("Arial", 16f, FontStyle.Bold);
        private bool _headerFontChanged = true;
        private Color _controlBackgroundColor = Color.White;
        private Color _tableBackgroundColor = Color.White;
        private Color _headerTextColor = Color.Black;
        private bool _headerTextColorChanged = true;
        private Color _header_bg_color = Color.DarkGray;
        private bool _header_bg_color_changed = true;



        private Font _line_font = new Font("Arial", 14f);
        private Font _selected_line_font = new Font("Arial", 14f);
        private Font _passed_line_font = new Font("Arial", 14f);

        private Color _line_text_color = Color.Black;
        private Color _selected_line_text_color = Color.Black;
        private Color _passed_line_text_color = Color.DarkGray;
        private Color _line_bg_color = Color.White;
        private Color _selected_line_bg_color = Color.Green;
        private Color _passed_line_bg_color = Color.Yellow;
        private int _buttons_size = 60;
        private Color _buttons_color;
        private bool _resize = true;
        private string _pathToRecipeFolder = "c:\\";
        private string _pathToXmlTableDefinition = @"c:\Distr\table.xml";
        private int currentRecipeLine = 2;
        private int prevCurrentRecipeLine = -2;
        private bool _to_runtime;

        private List<TableColumn> columns = new List<TableColumn>();

        private string make_table_msg = "_ ";
        private DateTime starttime = DateTime.Now;
        private int make_upload;

        private uint totalTimeLeft = 0;

        private int[] actLoopCount = new int[5];

        public int[] LoopCount

        {
            get => actLoopCount;
            set
            {
                if (value != actLoopCount)
                {
                    RecipeTime.SetData(_tableData, dataGridView1);
                    RecipeTime.Recalculate();
                }

                actLoopCount = value;
            }
        }


        RecipeLineFactory factory = new RecipeLineFactory();

        private List<RecipeLine> _tableData = new List<RecipeLine>(); //хранит состояние таблицы отдельно от dataGridView
        #endregion

        #region Properties

        [DisplayName("Режим")]
        public TableMode table_type
        {
            get => this._tableType;
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
            get => this._controlBackgroundColor;
            set
            {
                if (value != Color.Transparent)
                    this._controlBackgroundColor = value;
                ((Control)this).BackColor = this._controlBackgroundColor;
                this.DbgMsg.BackColor = this._controlBackgroundColor;
            }
        }

        [DisplayName("Цвет фона таблицы")]
        public Color table_bg_color
        {
            get => this._tableBackgroundColor;
            set
            {
                if (value != Color.Transparent)
                    this._tableBackgroundColor = value;
                this.dataGridView1.BackgroundColor = this._tableBackgroundColor;
            }
        }

        [DisplayName("Шрифт заголовка таблицы")]
        public Font header_font
        {
            get => this._headerFont;
            set
            {
                this._headerFont = value;
                this._headerFontChanged = true;
            }
        }

        [DisplayName("Цвет текста заголовка таблицы")]
        public Color header_text_color
        {
            get => this._headerTextColor;
            set
            {
                if (value != Color.Transparent)
                    this._headerTextColor = value;
                this._headerTextColorChanged = true;
            }
        }

        [DisplayName("Цвет фона заголовка таблицы")]
        public Color header_bg_color
        {
            get => this._header_bg_color;
            set
            {
                if (value != Color.Transparent)
                    this._header_bg_color = value;
                this._header_bg_color_changed = true;
            }
        }

        [DisplayName("Шрифт строки таблицы")]
        public Font line_font
        {
            get => this._line_font;
            set
            {
                this._line_font = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Цвет текста строки таблицы")]
        public Color line_text_color
        {
            get => this._line_text_color;
            set
            {
                if (value != Color.Transparent)
                    this._line_text_color = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Цвет фона строки таблицы")]
        public Color line_bg_color
        {
            get => this._line_bg_color;
            set
            {
                if (value != Color.Transparent)
                    this._line_bg_color = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Шрифт текущей строки таблицы")]
        public Font selected_line_font
        {
            get => this._selected_line_font;
            set
            {
                this._selected_line_font = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Цвет текста текущей строки таблицы")]
        public Color selected_line_text_color
        {
            get => this._selected_line_text_color;
            set
            {
                if (value != Color.Transparent)
                    this._selected_line_text_color = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Цвет фона текущей строки таблицы")]
        public Color selected_line_bg_color
        {
            get => this._selected_line_bg_color;
            set
            {
                if (value != Color.Transparent)
                    this._selected_line_bg_color = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Шрифт пройденной строки таблицы")]
        public Font passed_line_font
        {
            get => this._passed_line_font;
            set
            {
                this._passed_line_font = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Цвет текста пройденной строки таблицы")]
        public Color passed_line_text_color
        {
            get => this._passed_line_text_color;
            set
            {
                if (value != Color.Transparent)
                    this._passed_line_text_color = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Цвет фона пройденной строки таблицы")]
        public Color passed_line_bg_color
        {
            get => this._passed_line_bg_color;
            set
            {
                if (value != Color.Transparent)
                    this._passed_line_bg_color = value;
                this.ChangeRowFont();
            }
        }

        [DisplayName("Размер кнопок")]
        public int buttons_size
        {
            get => this._buttons_size;
            set
            {
                this._buttons_size = value;
                this._resize = true;
            }
        }

        [DisplayName("Цвет кнопок")]
        public Color buttons_color
        {
            get => this._buttons_color;
            set
            {
                if (value != Color.Transparent)
                    this._buttons_color = value;
                this.button_open.BackColor = this._buttons_color;
                this.button_save.BackColor = this._buttons_color;
            }
        }

        [DisplayName("Путь к рецептам")]
        public string init_path
        {
            get => this._pathToRecipeFolder;
            set
            {
                this._pathToRecipeFolder = value;
                this.openFileDialog1.InitialDirectory = this._pathToRecipeFolder;
                this.saveFileDialog1.InitialDirectory = this._pathToRecipeFolder;
            }
        }

        [DisplayName("XML файл с описанием таблицы")]
        public string table_definition
        {
            get => this._pathToXmlTableDefinition;
            set
            {
                this._pathToXmlTableDefinition = value;
                this.WriteStatusMessage("Описание таблицы будет изменено", false);
                //this.make_table(!this.FBConnector.DesignMode && this._table_type == table_edit.edit);
                this.ConfigureColumns(this._tableType == TableMode.Edit);
                this.WriteStatusMessage("Описание таблицы изменено", false);
            }
        }
        #endregion

        // ============================================================

        #region Constructor
        public TableControl() : base(true)
        {
            InitializeComponent();
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            //dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;//todo: check later       
        }
        #endregion

        #region TableView
        private void ChangeRowFont()
        {
            int index;
            for (index = 0; index < this.dataGridView1.Rows.Count && index < this.currentRecipeLine; ++index)
            {
                this.dataGridView1.Rows[index].DefaultCellStyle.BackColor = this._passed_line_bg_color;
                this.dataGridView1.Rows[index].DefaultCellStyle.Font = this._passed_line_font;
                this.dataGridView1.Rows[index].DefaultCellStyle.ForeColor = this._passed_line_text_color;
            }
            for (; index < this.dataGridView1.Rows.Count && index < this.currentRecipeLine + 1; ++index)
            {
                this.dataGridView1.Rows[index].DefaultCellStyle.BackColor = this._selected_line_bg_color;
                this.dataGridView1.Rows[index].DefaultCellStyle.Font = this._selected_line_font;
                this.dataGridView1.Rows[index].DefaultCellStyle.ForeColor = this._selected_line_text_color;
            }
            for (; index < this.dataGridView1.Rows.Count; ++index)
            {
                this.dataGridView1.Rows[index].DefaultCellStyle.BackColor = this._line_bg_color;
                this.dataGridView1.Rows[index].DefaultCellStyle.Font = this._line_font;
                this.dataGridView1.Rows[index].DefaultCellStyle.ForeColor = this._line_text_color;
            }
        }

        private void FillColumns(List<TableColumn> columns)
        {
            columns.Clear();
            foreach (TableColumn column in columns)
            {
                DataGridViewTextBoxColumn viewTextBoxColumn = new DataGridViewTextBoxColumn();
                viewTextBoxColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
                viewTextBoxColumn.Name = column.Name;
                viewTextBoxColumn.Tag = (object)column;
                column.GridIndex = this.dataGridView1.Columns.Add((DataGridViewColumn)viewTextBoxColumn);
            }
        }

        /// <summary>
        /// Конфигурирует ширину, тип и названия столбцов.
        /// </summary>
        private void ConfigureColumns(bool editMode)
        {
            WriteStatusMessage("Подготовка таблицы. ");
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            dataGridView1.RowHeadersWidth = _rowHeadersWidth;

            columns = RecipeLine.ColumnHeaders;
            foreach (TableColumn column in columns)
            {
                if (editMode)
                {
                    if (column.type == CellType._bool)
                    {
                        DataGridViewComboBoxColumn viewComboBoxColumn = new()
                        {
                            SortMode = DataGridViewColumnSortMode.NotSortable,
                            Name = column.Name,
                            Tag = column,
                            //Width = GetWidth(column),
                            MaxDropDownItems = 2
                        };
                        viewComboBoxColumn.Items.AddRange("Да", "Нет");

                        column.GridIndex = dataGridView1.Columns.Add(viewComboBoxColumn);
                    }
                    else if (column.type == CellType._enum)
                    {
                        DataGridViewComboBoxColumn viewComboBoxColumn = new()
                        {
                            SortMode = DataGridViewColumnSortMode.NotSortable,
                            Name = column.Name,
                            Tag = column,
                            //Width = GetWidth(column)
                        };

                        if (column.EnumType != null && column.GridIndex == Params.CommandIndex)
                        {
                            viewComboBoxColumn.MaxDropDownItems = column.EnumType.EnumCount;

                            for (int ittr_num = 0; ittr_num < column.EnumType.EnumCount; ++ittr_num)
                            {
                                viewComboBoxColumn.Items.Add(column.EnumType.GetValueByIndex(ittr_num));
                            }
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
                            //Width = GetWidth(column)
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
                        //Width = GetWidth(column)
                    };

                    column.GridIndex = dataGridView1.Columns.Add(viewTextBoxColumn);
                }
            }

            ChangeCellAlignment();

            dataGridView1.Columns[Params.CommandIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[Params.NumberIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[Params.SetpointIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[Params.TimeSetpointIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[Params.RecipeTimeIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[Params.CommentIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            WriteStatusMessage("Таблица подготовлена. ");
        }

        /// <summary>
        /// Возвращает ширину столбца в зависимости от имени. 
        /// Код в этом методе нарушает положения Женевской конвенции, его обязательно надо будет переделать
        /// </summary>
        private int GetWidth(TableColumn column)
        {
            switch (column.Name)
            {
                case "Действие":
                    return 200;
                case "Номер":
                    return 150;
                case "Задание":
                    return 150;
                case "Скорость/Время":
                    return 200;
                case "Время":
                    return 150;
                default:
                    return dataGridView1.Width - 780 - _rowHeadersWidth - 2 - 80; // Комментарий
            }
        }

        private void ChangeCellAlignment()
        {
            for (int cellIndex = 1; cellIndex < Params.ColumnCount - 1; cellIndex++)
            {
                dataGridView1.Columns[cellIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns[cellIndex].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            dataGridView1.RowHeadersWidth = _rowHeadersWidth;
        }


        private void BlockCells(int rowIndex)
        {
            for (int cellIndex = 1; cellIndex < Params.ColumnCount - 1; cellIndex++)
            {
                if (_tableData[rowIndex].GetCells[cellIndex].Type == CellType._blocked)
                {
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].ReadOnly = true;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Style.BackColor = Color.LightGray;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Value = "";
                }
                else
                {
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].ReadOnly = false;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Style.BackColor = Color.White;
                    dataGridView1.Rows[rowIndex].Cells[cellIndex].Value = _tableData[rowIndex].GetCells[cellIndex].GetValue();
                }
            }
        }

        private void WriteStatusMessage(string message, bool isError = false)
        {
            this.DbgMsg.Text = message;
            this.DbgMsg.BackColor = isError ? Color.OrangeRed : Color.White;
        }

        #endregion

        #region OnPaint
        protected override void OnPaint(PaintEventArgs e)
        {
            if (this._resize)
            {
                this._resize = false;
            }
            if (this._headerFontChanged)
            {
                this.dataGridView1.RowHeadersDefaultCellStyle.Font = this._headerFont;
                this.dataGridView1.ColumnHeadersDefaultCellStyle.Font = this._headerFont;
                this._headerFontChanged = false;
            }
            if (this._headerTextColorChanged)
            {
                this.dataGridView1.RowHeadersDefaultCellStyle.ForeColor = this._headerTextColor;
                this.dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = this._headerTextColor;
                this._headerTextColorChanged = false;
            }
            if (this._header_bg_color_changed)
            {
                this.dataGridView1.RowHeadersDefaultCellStyle.BackColor = this._header_bg_color;
                this.dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = this._header_bg_color;
                this._header_bg_color_changed = false;
            }
            if (this._to_runtime)
            {
                this.currentRecipeLine = -1;
                this.prevCurrentRecipeLine = -2;
                this._to_runtime = false;
            }
            if (this.currentRecipeLine != this.prevCurrentRecipeLine)
                this.ChangeRowFont();
            this.prevCurrentRecipeLine = this.currentRecipeLine;
            if (this.FBConnector.DesignMode)
                return;

            var shutterPins = new ReadPins(Params.FirstPinShutterName, Params.ShutterNameQuantity, FBConnector.Fb as MbeTableFB);
            if (shutterPins.IsPinGroupQualityGood())
            {
                TableEnumType shutterNames = shutterPins.ReadPinNames();
                GrowthList.SetShutterNames(shutterNames);
            }

            var heaterPins = new ReadPins(Params.FirstPinHeaterName, Params.HeaterNameQuantity, FBConnector.Fb as MbeTableFB);
            if (heaterPins.IsPinGroupQualityGood())
            {
                TableEnumType heaterNames = heaterPins.ReadPinNames();
                GrowthList.SetHeaterNames(heaterNames);
            }

            uint pinValue1 = ((FBBase)this.FBConnector).GetPinValue<uint>(Params.ID_HMI_Status);
            OpcQuality pinQuality1 = ((FBBase)this.FBConnector).GetPinQuality(Params.ID_HMI_Status);
            int pinValue2 = ((FBBase)this.FBConnector).GetPinValue<int>(Params.ID_HMI_ActualLine);
            OpcQuality pinQuality2 = ((FBBase)this.FBConnector).GetPinQuality(Params.ID_HMI_ActualLine);

            this.button_save.Visible = true;
            if (pinQuality2 != OpcQuality.Good || pinQuality1 != OpcQuality.Good || ((int)pinValue1 & 4) != 4)
                return;
            currentRecipeLine = pinValue2;

            if (currentRecipeLine != prevCurrentRecipeLine)
            {
                ChangeRowFont();
                prevCurrentRecipeLine = currentRecipeLine;
            }
        }

        private void ChangeToViewMode()
        {
            this.button_open.Enabled = true;
        }

        private void ChangeToEditMode()
        {
            this.button_open.Enabled = true;
            this.button_save.Visible = true;
            this.button_save.Enabled = true;
            this.button_del.Visible = true;
            this.button_add_after.Visible = true;
            this.button_add_before.Visible = true;
        }
        #endregion


        #region MasterScada methods override
        protected override void ToDesign()
        {
            ConfigureColumns(false);
            currentRecipeLine = 2;
            ChangeRowFont();
            WriteStatusMessage(this.make_table_msg, false);
            make_upload = 0;
        }
        protected override void ToRuntime()
        {
            _to_runtime = true;
            DbgMsg.Text = "";
            ConfigureColumns(this._tableType == TableMode.Edit);
            make_upload = 1;
            dataGridView1.ReadOnly = this._tableType == TableMode.View;
            _tableData.Clear();
        }
        #endregion


        private void MainTable_Click(object sender, EventArgs e)
        {
            int num = this.FBConnector.DesignMode ? 1 : 0;
        }

        private void UpdateEnumDropDown(DataGridViewComboBoxCell cell, object cellValue)
        {
            try
            {
                string actionType = GrowthList.GetActionType(cellValue.ToString());

                cell.Items.Clear();

                // Заполняем комбобокс в зависимости от типа действия
                if (actionType == "shutter")
                {
                    cell.MaxDropDownItems = GrowthList.ShutterNames.EnumCount;
                    for (int ittr_num = 0; ittr_num < GrowthList.ShutterNames.EnumCount; ++ittr_num)
                    {
                        cell.Items.Add(GrowthList.ShutterNames.GetValueByIndex(ittr_num));
                    }
                }
                else if (actionType == "heater")
                {
                    cell.MaxDropDownItems = GrowthList.HeaterNames.EnumCount;
                    for (int ittr_num = 0; ittr_num < GrowthList.HeaterNames.EnumCount; ++ittr_num)
                    {
                        cell.Items.Add(GrowthList.HeaterNames.GetValueByIndex(ittr_num));
                    }
                }
                else
                {
                    cell.MaxDropDownItems = 1;
                    cell.Items.Add(cellValue);
                }

                if (cell.Items.Contains(cellValue))
                {
                    cell.Value = cellValue;
                }
                else
                {
                    cell.Value = cell.Items.Count > 0 ? cell.Items[0] : null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении комбобокса: {ex.Message}");
            }
        }



        #region CellChange
        private void EndCellEdit(object sender, DataGridViewCellEventArgs e)
        {
            //Проверка данных по окончании редактирования таблицы

            int columnIndex = e.ColumnIndex;
            int rowIndex = e.RowIndex;

            var currentCell = dataGridView1.Rows[rowIndex].Cells[columnIndex];

            if (columnIndex == Params.NumberIndex)
            {
                string currentAction = (string)dataGridView1.Rows[rowIndex].Cells[Params.CommandIndex].Value;

                if (ActionManager.GetTargetAction(currentAction) == ActionType.Shutter)
                {
                    _tableData[rowIndex].ChangeNumber(currentCell.Value.ToString());
                }
                else if (ActionManager.GetTargetAction(currentAction) == ActionType.Heater)
                {
                    _tableData[rowIndex].ChangeNumber(currentCell.Value.ToString());
                }
                else
                {
                    _tableData[rowIndex].ChangeNumber("");
                }
                RefreshTable();
            }
            else if (columnIndex == Params.SetpointIndex)
            {
                bool isParseOk = float.TryParse(currentCell.Value.ToString(), out float newValue);

                if (!_tableData[rowIndex].ChangeSetpoint(newValue) || !isParseOk)
                {
                    currentCell.Value = _tableData[rowIndex].GetSetpoint();

                    MessageBox.Show($"Введите число: \n" +
                                    $"минимальное значение: {_tableData[rowIndex].MinSetpoint}\n" +
                                    $"максимальное значение: {_tableData[rowIndex].MaxSetpoint}\n",
                                    $"Ошибка ввода данных:",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
                else
                {
                    currentCell.Value = _tableData[rowIndex].GetCells[columnIndex].GetValue();
                    RefreshTable();
                }
            }
            else if (columnIndex == Params.TimeSetpointIndex)
            {
                // Заполняет пустой пользовательский ввод нулем в datagridview
                string text = currentCell?.Value?.ToString() ?? "0";

                bool isParseOk = DateTimeParser.TryParse(text, out var newValue) || newValue < _tableData[rowIndex].MinTimeSetpoint || newValue > _tableData[rowIndex].MaxTimeSetpoint;

                if (!_tableData[rowIndex].ChangeSpeed(newValue) || !isParseOk)
                {
                    currentCell.Value = _tableData[rowIndex].GetTime();
                    MessageBox.Show($"Введите число: \n" +
                                    $"минимальное значение: {_tableData[rowIndex].MinTimeSetpoint} с\n" +
                                    $"максимальное значение: {_tableData[rowIndex].MaxTimeSetpoint} с\n",
                                    $"Ошибка ввода данных:",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
                else
                {
                    currentCell.Value = _tableData[rowIndex].GetCells[columnIndex].GetValue();
                    RefreshTable();
                }
            }
            else if (columnIndex == Params.CommentIndex)
            {
                string newValue = (string)currentCell.Value;
                _tableData[rowIndex].ChangeComment(newValue);
            }
        }
        void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int columnIndex = e.ColumnIndex;
            int rowIndex = e.RowIndex;

            DataGridViewComboBoxCell comboBox = (DataGridViewComboBoxCell)dataGridView1.Rows[rowIndex].Cells[Params.CommandIndex];

            if (comboBox.Value != null && columnIndex == Params.CommandIndex && !isLoadingActive)
            {
                dataGridView1.Rows[rowIndex].HeaderCell.Value = (rowIndex + 1).ToString();

                if (columnIndex == Params.CommandIndex)
                {
                    string command = (string)dataGridView1.Rows[rowIndex].Cells[columnIndex].Value;
                    int minNumber = GrowthList.GetMinNumber(command);

                    ReplaceLineInRecipe(factory.NewLine(command, minNumber, 0f, 0f, ""));
                }
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
                    _loadDelay.Interval = 100;

                    _loadDelay.Tick += (object sender2, EventArgs e2) =>
                    {
                        if (TryLoadRecipeFromPlc())
                            WriteStatusMessage("Рецепт загружен из ПЛК");
                        else
                            WriteStatusMessage("Не удалось загрузить рецепт из ПЛК");

                        _loadDelay.Stop();
                    };
                }

                _loadDelay.Start();
            }
        }
    }
}
