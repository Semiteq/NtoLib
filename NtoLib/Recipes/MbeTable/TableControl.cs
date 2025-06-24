using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.IO;
using NtoLib.Recipes.MbeTable.PLC;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.RecipeLines;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Table;
using NtoLib.Recipes.MbeTable.Table.UI.StatusManager;
using NtoLib.Recipes.MbeTable.Table.UI.TableUpdate;

namespace NtoLib.Recipes.MbeTable
{
    [ComVisible(true)]
    [DisplayName("Таблица рецептов МБЕ")]
    [Guid("8161DF32-8D80-4B81-AF52-3021AE0AD293")]
    public partial class TableControl : VisualControlBase
    {
        #region Private fields

        private DataGridView _table;
        private Button _buttonAddAfter;
        private Button _buttonAddBefore;
        private Button _buttonDel;
        private Button _buttonSave;
        private Button _buttonOpen;

        private readonly List<Step> _recipe = new();
        private TableMode _tableType = TableMode.Edit;
        
        // Managers
        private StatusManager _statusManager;
        private RecipeManager _recipeManager;
        private TableManager _tableManager;

        // Classes 
        private OpenFileDialog _openFileDialog1;
        private SaveFileDialog _saveFileDialog1;
        private ColorScheme _colorScheme;
        private TableSchema _tableSchema;
        private TablePainter _tablePainter;
        private UpdateBatcher _updateBatcher;
        
        // Table background colors
        private Color _controlBackgroundColor = Color.White;
        private Color _tableBackgroundColor = Color.White;
        
        // Table fonts
        private Font _headerFont = new("Arial", 16f, FontStyle.Bold);
        private Font _lineFont = new("Arial", 14f);
        private Font _selectedLineFont = new("Arial", 14f);
        private Font _passedLineFont = new("Arial", 14f);

        // Line text colors
        private Color _headerTextColor = Color.Black;
        private Color _lineTextColor = Color.Black;
        private Color _selectedLineTextColor = Color.Black;
        private Color _passedLineTextColor = Color.DarkGray;
        
        // Line background colors
        private Color _headerBgColor = Color.DarkGray;
        private Color _lineBgColor = Color.White;
        private Color _selectedLineBgColor = Color.Green;
        private Color _passedLineBgColor = Color.Yellow;
        
        // Buttons
        private Color _buttonsColor = Color.Gray;
        private int _buttonsSize = 24;
        
        #endregion

        #region Properties

        [DisplayName("Режим")]
        public TableMode TableType
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
        public Color ControlBgColor
        {
            get => _controlBackgroundColor;
            set
            {
                if (value != Color.Transparent)
                    _controlBackgroundColor = value;
                
                BackColor = _controlBackgroundColor;
                DbgMsg.BackColor = _controlBackgroundColor;

                _colorScheme.ControlBackgroundColor = value;
            }
        }

        [DisplayName("Цвет фона таблицы")]
        public Color TableBgColor
        {
            get => _tableBackgroundColor;
            set
            {
                if (value != Color.Transparent)
                    _tableBackgroundColor = value;
                
                _table.BackgroundColor = _tableBackgroundColor;
                
                _colorScheme.TableBackgroundColor = value;
            }
        }

        [DisplayName("Шрифт заголовка таблицы")]
        public Font HeaderFont
        {
            get => _headerFont;
            set
            {
                _headerFont = value;
                _colorScheme.HeaderFont = value;
            }
        }

        [DisplayName("Цвет текста заголовка таблицы")]
        public Color HeaderTextColor
        {
            get => _headerTextColor;
            set
            {
                if (value != Color.Transparent)
                    _headerTextColor = value;
                
                _colorScheme.HeaderTextColor = value;
            }
        }

        [DisplayName("Цвет фона заголовка таблицы")]
        public Color HeaderBgColor
        {
            get => _headerBgColor;
            set
            {
                if (value != Color.Transparent)
                    _headerBgColor = value;
                
                _colorScheme.HeaderBgColor = value;
            }
        }

        [DisplayName("Шрифт строки таблицы")]
        public Font LineFont
        {
            get => _lineFont;
            set
            {
                _lineFont = value;
                _colorScheme.LineFont = value;
            }
        }

        [DisplayName("Цвет текста строки таблицы")]
        public Color LineTextColor
        {
            get => _lineTextColor;
            set
            {
                if (value != Color.Transparent)
                    _lineTextColor = value;
                
                _colorScheme.LineTextColor = value;
            }
        }

        [DisplayName("Цвет фона строки таблицы")]
        public Color LineBgColor
        {
            get => _lineBgColor;
            set
            {
                if (value != Color.Transparent)
                    _lineBgColor = value;
                
                _colorScheme.LineBgColor = value;
            }
        }

        [DisplayName("Шрифт текущей строки таблицы")]
        public Font SelectedLineFont
        {
            get => _selectedLineFont;
            set
            {
                _selectedLineFont = value; 
                _colorScheme.SelectedLineFont = value;
            }
        }

        [DisplayName("Цвет текста текущей строки таблицы")]
        public Color SelectedLineTextColor
        {
            get => _selectedLineTextColor;
            set
            {
                if (value != Color.Transparent)
                    _selectedLineTextColor = value;
                
                _colorScheme.SelectedLineTextColor = value;
            }
        }

        [DisplayName("Цвет фона текущей строки таблицы")]
        public Color SelectedLineBgColor
        {
            get => _selectedLineBgColor;
            set
            {
                if (value != Color.Transparent)
                    _selectedLineBgColor = value;
                
                _colorScheme.SelectedLineBgColor = value;
            }
        }

        [DisplayName("Шрифт пройденной строки таблицы")]
        public Font PassedLineFont
        {
            get => _passedLineFont;
            set
            {
                _passedLineFont = value; 
                _colorScheme.PassedLineFont = value;
            }
        }

        [DisplayName("Цвет текста пройденной строки таблицы")]
        public Color PassedLineTextColor
        {
            get => _passedLineTextColor;
            set
            {
                if (value != Color.Transparent)
                    _passedLineTextColor = value;
                
                _colorScheme.PassedLineTextColor = value;
            }
        }

        [DisplayName("Цвет фона пройденной строки таблицы")]
        public Color PassedLineBgColor
        {
            get => _passedLineBgColor;
            set
            {
                if (value != Color.Transparent)
                    _passedLineBgColor = value;
                
                _colorScheme.PassedLineBgColor = value;
            }
        }

        [DisplayName("Размер кнопок")]
        public int ButtonsSize
        {
            get => _buttonsSize;
            set
            {
                _buttonsSize = value; 
                _colorScheme.ButtonsSize = value;
            }
        }

        [DisplayName("Цвет кнопок")]
        public Color ButtonsColor
        {
            get => _buttonsColor;
            set
            {
                if (value != Color.Transparent)
                    _buttonsColor = value;
                
                _buttonOpen.BackColor = _buttonsColor;
                _buttonSave.BackColor = _buttonsColor;
                
                _colorScheme.ButtonsColor = value;
            }
        }

        #endregion

        #region Constructor

        public TableControl() : base(true)
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            InitializeDataGrid();
            InitializeDialogs();
            InitializeServices();
            InitializeManagers();
            SetupEventHandlers();
        }

        private void InitializeDataGrid()
        {
            _table.Rows.Clear();
            _table.Columns.Clear();
            _table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _table.RowHeadersWidth = 90;
        }

        private void InitializeDialogs()
        {
            _openFileDialog1 = new OpenFileDialog
            {
                Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                AddExtension = true,
                Multiselect = false,
                Title = @"Выберите файл рецепта",
                RestoreDirectory = true
            };

            _saveFileDialog1 = new SaveFileDialog
            {
                Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                AddExtension = true,
                Title = @"Сохраните файл рецепта",
                RestoreDirectory = true
            };
        }

        private void InitializeServices()
        {
            _tableSchema = new();
            
            _plcCommunication = new PlcCommunication();
            _communicationSettings = new CommunicationSettings();

            _recipeFileReader = new RecipeFileReader();
            _recipeFileWriter = new RecipeFileWriter();

            _tablePainter = new TablePainter(_table);
            _updateBatcher = new UpdateBatcher(_table, _recipe);
        }

        private void InitializeManagers()
        {
            _statusManager = new StatusManager(DbgMsg);
        }

        private void RegisterComponents()
        {
            if (FBConnector.Fb is not MbeTableFB fb) return;

            fb.RegisterTableData(_recipe);
            fb.RegisterDataGridViewUpdater(_updateBatcher);
            fb.RegisterCommunicationSettings(_communicationSettings);
            fb.RegisterShutters(_shutters);
            fb.RegisterHeaters(_heaters);
            fb.RegisterNitrogenSources(_nitrogenSources);
        }

        #endregion

        #region Event Handlers

        private void ClickButton_Delete(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View) return;
            var selectedRowIndex = _table.CurrentRow?.Index ?? -1;

            if (!_recipeManager.TryRemoveStep(selectedRowIndex, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
                return;
            }
            
            _tableManager.RemoveLine(selectedRowIndex);
        }

        private void ClickButton_AddLineBefore(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View) return;

            var selectedRowIndex = _table.CurrentRow?.Index ?? 0;
            
            if (!_recipeManager.TryAddNewStep(selectedRowIndex, out var step, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
                return;
            }

            _tableManager.AddLine(step, selectedRowIndex);
        }

        private void ClickButton_AddLineAfter(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _tableType == TableMode.View) return;

            var selectedRowIndex = _table.CurrentRow?.Index ?? 0;

            if (!_recipeManager.TryAddNewStep(selectedRowIndex + 1, out var step, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
                return;
            }
            
            _tableManager.AddLine(step, selectedRowIndex);
        }

        private void ClickButton_Open(object sender, EventArgs e)
        {
            if (this.FBConnector.DesignMode || _openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            _fileManager.LoadRecipe(_openFileDialog1.FileName);
        }

        private void ClickButton_Save(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode) return;

            if (_saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            _fileManager.SaveRecipe(_saveFileDialog1.FileName);
        }

        // Handles cell edit completion event
        private void EndCellEdit(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void OnRowHeaderDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            _loopManager.OnRowHeaderDoubleClick(sender, e);
        }

        // Handles immediate cell edit state changes for real-time processing
        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {

        }

        // Handles cell edit completion (fires when editing ends, regardless of value change)
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _table.Rows.Count)
                return;

            var rowIndex = e.RowIndex;
            var columnIndex = e.ColumnIndex;
            var cell = _table.Rows[rowIndex].Cells[columnIndex];
            
            
            
            if (!_recipeManager.TrySetStepProperty(rowIndex, columnIndex, cell.Value, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
                return;
            }
            
            _tableManager.UpdateCell(rowIndex, columnIndex, cell.Value);
        }

        private async void HandleVisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (!Visible || DesignMode || _tableType != TableMode.View) return;
                if (await _fileManager.TryLoadRecipeFromPlc())
                    _statusManager.WriteStatusMessage("Рецепт загружен из ПЛК");
            }
            catch (Exception ex)
            {
                _statusManager.WriteStatusMessage($"Ошибка загрузки рецепта: {ex.Message}", true);
            }
        }

        private void SetupEventHandlers()
        {
            _table.RowHeaderMouseDoubleClick += OnRowHeaderDoubleClick;
        }

        #endregion

        #region OnPaint

        protected override void OnPaint(PaintEventArgs e)
        {
            if (FBConnector.DesignMode || FBConnector.Fb is not MbeTableFB) return;

            _updateBatcher.ProcessUpdates();
        }

        private void ChangeToViewMode()
        {
            _buttonOpen.Enabled = true;
        }

        private void ChangeToEditMode()
        {
            _buttonOpen.Enabled = true;
            
            _buttonSave.Visible = true;
            _buttonSave.Enabled = true;
            
            _buttonDel.Visible = true;
            
            _buttonAddAfter.Visible = true;
            _buttonAddBefore.Visible = true;
        }

        #endregion

        protected override void ToDesign()
        {
            
            _statusManager.ClearStatusMessage();
            _tableManager.ToDesign();
        }

        protected override void ToRuntime()
        {
            _statusManager.ClearStatusMessage();
            _tableManager.ToRuntime();
            
        }
    }
}

