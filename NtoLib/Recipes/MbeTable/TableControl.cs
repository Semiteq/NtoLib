using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.StatusManager;
using NtoLib.Recipes.MbeTable.Table;

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

        private bool _isInitialized = false;

        // Managers
        private StatusManager.StatusManager _statusManager;
        private TableColumnManager _tableColumnManager;
        private ActionManager _actionManager = new();

        // Classes
        private RecipeViewModel _recipeViewModel;
        private ComboBoxDataProvider _dataProvider;

        private OpenFileDialog _openFileDialog1;
        private SaveFileDialog _saveFileDialog1;

        private TableSchema _tableSchema = new();
        private TablePainter _tablePainter;

        private TableCellFormatter _tableCellFormatter;

        private ColorScheme _colorScheme = new();

        #endregion

        #region Properties

        [DisplayName("Цвет фона")]
        public Color ControlBgColor
        {
            get => _colorScheme.ControlBackgroundColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.ControlBackgroundColor = value;
                BackColor = value;
                DbgMsg.BackColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет фона таблицы")]
        public Color TableBgColor
        {
            get => _colorScheme.TableBackgroundColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.TableBackgroundColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Шрифт заголовка таблицы")]
        public Font HeaderFont
        {
            get => _colorScheme.HeaderFont;
            set
            {
                if (Equals(_colorScheme.HeaderFont, value)) return;
                _colorScheme.HeaderFont = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет текста заголовка таблицы")]
        public Color HeaderTextColor
        {
            get => _colorScheme.HeaderTextColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.HeaderTextColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет фона заголовка таблицы")]
        public Color HeaderBgColor
        {
            get => _colorScheme.HeaderBgColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.HeaderBgColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Шрифт строки таблицы")]
        public Font LineFont
        {
            get => _colorScheme.LineFont;
            set
            {
                if (Equals(_colorScheme.LineFont, value)) return;
                _colorScheme.LineFont = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет текста строки таблицы")]
        public Color LineTextColor
        {
            get => _colorScheme.LineTextColor;
            set
            {
                if (_colorScheme.LineBgColor == value) return;
                _colorScheme.LineBgColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет фона строки таблицы")]
        public Color LineBgColor
        {
            get => _colorScheme.LineBgColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.LineBgColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Шрифт текущей строки таблицы")]
        public Font SelectedLineFont
        {
            get => _colorScheme.SelectedLineFont;
            set
            {
                if (Equals(_colorScheme.SelectedLineFont, value)) return;
                _colorScheme.SelectedLineFont = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет текста текущей строки таблицы")]
        public Color SelectedLineTextColor
        {
            get => _colorScheme.SelectedLineTextColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.SelectedLineTextColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет фона текущей строки таблицы")]
        public Color SelectedLineBgColor
        {
            get => _colorScheme.SelectedLineBgColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.SelectedLineBgColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Шрифт пройденной строки таблицы")]
        public Font PassedLineFont
        {
            get => _colorScheme.PassedLineFont;
            set
            {
                if (Equals(_colorScheme.PassedLineFont, value)) return;
                _colorScheme.PassedLineFont = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет текста пройденной строки таблицы")]
        public Color PassedLineTextColor
        {
            get => _colorScheme.PassedLineTextColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.PassedLineTextColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет фона пройденной строки таблицы")]
        public Color PassedLineBgColor
        {
            get => _colorScheme.PassedLineBgColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.PassedLineBgColor = value;
                UpdateUiManagers();
            }
        }

        [DisplayName("Цвет кнопок")]
        public Color ButtonsColor
        {
            get => _colorScheme.ButtonsColor;
            set
            {
                if (value == Color.Transparent) return;
                _colorScheme.ButtonsColor = value;
                _buttonOpen.BackColor = value;
                _buttonSave.BackColor = value;
                UpdateUiManagers();
            }
        }

        #endregion

        #region Constructor

        public TableControl() : base(true)
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_isInitialized)
            {
                InitializeControl(FBConnector.DesignMode);
            }
        }

        protected override void ToRuntime()
        {
            _statusManager?.ClearStatusMessage();
            InitializeControl(false);
        }

        protected override void ToDesign()
        {
            _statusManager?.ClearStatusMessage();
            InitializeControl(true);
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

        private void SetupEventHandlers()
        {
            _table.CellFormatting += Table_CellFormatting;
            _table.CellBeginEdit += Table_CellBeginEdit;
            _table.DataError += Table_DataError;
        }

        private void Table_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            _statusManager.WriteStatusMessage($"DataError in [{e.RowIndex}, {e.ColumnIndex}]: {e.Exception.Message}", StatusMessage.Error);
            //Console.WriteLine($"DataError in [{e.RowIndex}, {e.ColumnIndex}]: {e.Exception.Message}");
            e.ThrowException = true;
        }

        #endregion

        private void Table_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var columnIndex = e.ColumnIndex;
            var rowIndex = e.RowIndex;

            if (rowIndex < 0 || rowIndex >= _recipeViewModel.ViewModels.Count)
                return;

            var viewModel = _recipeViewModel.ViewModels[rowIndex];
            var columnKey = _tableSchema.GetColumnKeyByIndex(columnIndex);
            var columnDef = _tableSchema.GetColumnDefinition(columnIndex);

            var actualLineNumber = -1; // todo: get from a source that provides runtime context
            var stateType = _tablePainter.GetStateType(viewModel, actualLineNumber, columnKey);

            _tablePainter.ApplyState(e.CellStyle, stateType);

            if (columnDef.TableCellType != typeof(DataGridViewComboBoxCell))
            {
                if (stateType != TablePainter.StateType.Blocked)
                {
                    e.Value = _tableCellFormatter.GetFormattedValue(viewModel, columnKey);
                }
            }
        }

        private void Table_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var rowIndex = e.RowIndex;
            if (rowIndex < 0) return;

            var columnIndex = e.ColumnIndex;
            var columnDef = _tableSchema.GetColumnDefinition(columnIndex);


            if (columnDef.TableCellType == typeof(DataGridViewComboBoxCell) && !columnDef.ComboBoxSource.IsStatic)
            {

                var viewModel = _recipeViewModel.ViewModels[rowIndex];


                if (_table[columnIndex, rowIndex] is DataGridViewComboBoxCell cell)
                {

                    var dynamicSource = _dataProvider.GetDynamicDataSource(columnDef.ComboBoxSource.DataSourceKey, viewModel);
                    cell.DataSource = new BindingSource(dynamicSource, null);
                }
            }
        }

        #region Button Click Handlers

        private void ClickButton_Delete(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode) return;
            var selectedRowIndex = _table.CurrentRow?.Index ?? -1;
            if (selectedRowIndex < 0) return;

            if (!_recipeViewModel.RemoveStep(selectedRowIndex, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
            }
        }

        private void ClickButton_AddLineBefore(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode) return;
            var selectedRowIndex = _table.CurrentRow?.Index ?? 0;

            if (!_recipeViewModel.AddNewStep(selectedRowIndex, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
            }
        }

        private void ClickButton_AddLineAfter(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode) return;
            var selectedRowIndex = _table.CurrentRow?.Index ?? 0;

            if (!_recipeViewModel.AddNewStep(selectedRowIndex + 1, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
            }
        }

        private void ClickButton_Open(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            // _fileManager.LoadRecipe(_openFileDialog1.FileName);
        }

        private void ClickButton_Save(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode) return;

            if (_saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            // _fileManager.SaveRecipe(_saveFileDialog1.FileName);
        }

        #endregion

        private void UpdateUiManagers()
        {
            if (_tableSchema == null || _dataProvider == null) return;

            _tableColumnManager = new TableColumnManager(_table, _tableSchema, _colorScheme, _dataProvider);
            _tablePainter = new TablePainter(_colorScheme);

            _tableColumnManager.InitializeHeaders();
            _tableColumnManager.InitializeTableColumns();
            _table.Invalidate();
        }

        private void InitializeControl(bool isDesignMode)
        {
            _statusManager = new StatusManager.StatusManager(DbgMsg);
            _tableSchema = new TableSchema();
            _actionManager = new ActionManager();
            _tableCellFormatter = new TableCellFormatter();

            IFbActionTarget fbTarget;
            if (isDesignMode || !(FBConnector.Fb is IFbActionTarget realFb))
            {
                fbTarget = new MockFbActionTarget();
            }
            else
            {
                fbTarget = realFb;
            }

            _dataProvider = new ComboBoxDataProvider(_actionManager, fbTarget);

            _recipeViewModel = new RecipeViewModel(_tableSchema, _actionManager, _dataProvider);
            _table.DataSource = _recipeViewModel.ViewModels;

            UpdateUiManagers();

            if (!_isInitialized)
            {
                SetupEventHandlers();
                InitializeDialogs();
            }

            _colorScheme = new ColorScheme
            {
                ControlBackgroundColor = Color.White,
                TableBackgroundColor = Color.White,

                HeaderFont = new Font("Arial", 16f, FontStyle.Bold),

                LineFont = new Font("Arial", 14f),
                SelectedLineFont = new Font("Arial", 14f),
                PassedLineFont = new Font("Arial", 14f),

                LineTextColor = Color.Black,
                SelectedLineTextColor = Color.Black,
                PassedLineTextColor = Color.DarkGray,

                LineBgColor = Color.White,
                SelectedLineBgColor = Color.Green,
                PassedLineBgColor = Color.Yellow,

                HeaderBgColor = Color.LightGray,
                HeaderTextColor = Color.Black,

                ButtonsColor = Color.LightGray,
            };

            _isInitialized = true;
        }
    }
}