using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.PinDataManager;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Status;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable
{
    [ComVisible(true)]
    [DisplayName("Таблица рецептов МБЕ")]
    [Guid("8161DF32-8D80-4B81-AF52-3021AE0AD293")]
    public partial class TableControl : VisualControlBase
    {
        [NonSerialized] private ServiceProvider _sp;

        [NonSerialized] private RecipeViewModel _recipeViewModel;
        [NonSerialized] private TableSchema _tableSchema;
        [NonSerialized] private IStatusManager _statusManager;
        [NonSerialized] private TablePainter _tablePainter;
        [NonSerialized] private TableCellFormatter _tableCellFormatter;
        [NonSerialized] private ComboBoxDataProvider _dataProvider;
        [NonSerialized] private OpenFileDialog _openFileDialog;
        [NonSerialized] private SaveFileDialog _saveFileDialog;
        [NonSerialized] private ColorScheme _colorScheme;
        [NonSerialized] private IPlcStateMonitor _plcStateMonitor;

        [NonSerialized] private Color _controlBgColor = Color.White;
        [NonSerialized] private Color _tableBgColor = Color.White;
        [NonSerialized] private Font _headerFont = new("Arial", 16f, FontStyle.Bold);
        [NonSerialized] private Color _headerTextColor = Color.Black;
        [NonSerialized] private Color _headerBgColor = Color.LightGray;
        [NonSerialized] private Font _lineFont = new("Arial", 14f);
        [NonSerialized] private Color _lineTextColor = Color.Black;
        [NonSerialized] private Color _lineBgColor = Color.White;
        [NonSerialized] private Font _selectedLineFont = new("Arial", 14f);
        [NonSerialized] private Color _selectedLineTextColor = Color.Black;
        [NonSerialized] private Color _selectedLineBgColor = Color.Green;
        [NonSerialized] private Font _passedLineFont = new("Arial", 14f);
        [NonSerialized] private Color _passedLineTextColor = Color.DarkGray;
        [NonSerialized] private Color _passedLineBgColor = Color.Yellow;
        [NonSerialized] private Color _buttonsColor = Color.LightGray;

        #region Properties

        [DisplayName("Цвет фона")]
        public Color ControlBgColor
        {
            get => _controlBgColor;
            set
            {
                if (_controlBgColor == value) return;

                _controlBgColor = value;
                BackColor = value;

                if (_labelStatus != null) _labelStatus.BackColor = value;
                if (_sp?.ColorScheme != null) _sp.ColorScheme.ControlBackgroundColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет фона таблицы")]
        public Color TableBgColor
        {
            get => _tableBgColor;
            set
            {
                if (_tableBgColor == value) return;

                _tableBgColor = value;

                if (_table != null) _table.BackgroundColor = value;
                if (_sp?.ColorScheme != null) _sp.ColorScheme.TableBackgroundColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Шрифт заголовка таблицы")]
        public Font HeaderFont
        {
            get => _headerFont;
            set
            {
                if (Equals(_headerFont, value)) return;

                _headerFont = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.HeaderFont = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет текста заголовка таблицы")]
        public Color HeaderTextColor
        {
            get => _headerTextColor;
            set
            {
                if (_headerTextColor == value) return;

                _headerTextColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.HeaderTextColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет фона заголовка таблицы")]
        public Color HeaderBgColor
        {
            get => _headerBgColor;
            set
            {
                if (_headerBgColor == value) return;

                _headerBgColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.HeaderBgColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Шрифт строки таблицы")]
        public Font LineFont
        {
            get => _lineFont;
            set
            {
                if (Equals(_lineFont, value)) return;

                _lineFont = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.LineFont = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет текста строки таблицы")]
        public Color LineTextColor
        {
            get => _lineTextColor;
            set
            {
                if (_lineTextColor == value) return;

                _lineTextColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.LineTextColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет фона строки таблицы")]
        public Color LineBgColor
        {
            get => _lineBgColor;
            set
            {
                if (_lineBgColor == value) return;

                _lineBgColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.LineBgColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Шрифт текущей строки таблицы")]
        public Font SelectedLineFont
        {
            get => _selectedLineFont;
            set
            {
                if (Equals(_selectedLineFont, value)) return;

                _selectedLineFont = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.SelectedLineFont = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет текста текущей строки таблицы")]
        public Color SelectedLineTextColor
        {
            get => _selectedLineTextColor;
            set
            {
                if (_selectedLineTextColor == value) return;

                _selectedLineTextColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.SelectedLineTextColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет фона текущей строки таблицы")]
        public Color SelectedLineBgColor
        {
            get => _selectedLineBgColor;
            set
            {
                if (_selectedLineBgColor == value) return;

                _selectedLineBgColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.SelectedLineBgColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Шрифт пройденной строки таблицы")]
        public Font PassedLineFont
        {
            get => _passedLineFont;
            set
            {
                if (Equals(_passedLineFont, value)) return;

                _passedLineFont = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.PassedLineFont = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет текста пройденной строки таблицы")]
        public Color PassedLineTextColor
        {
            get => _passedLineTextColor;
            set
            {
                _passedLineTextColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.PassedLineTextColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет фона пройденной строки таблицы")]
        public Color PassedLineBgColor
        {
            get => _passedLineBgColor;
            set
            {
                if (_passedLineBgColor == value) return;

                _passedLineBgColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.PassedLineBgColor = value;

                UpdateColorScheme();
            }
        }

        [DisplayName("Цвет кнопок")]
        public Color ButtonsColor
        {
            get => _buttonsColor;
            set
            {
                if (_buttonsColor == value) return;

                _buttonsColor = value;

                if (_buttonOpen != null) _buttonOpen.BackColor = value;
                if (_buttonSave != null) _buttonSave.BackColor = value;
                if (_buttonAddBefore != null) _buttonAddBefore.BackColor = value;
                if (_buttonAddAfter != null) _buttonAddAfter.BackColor = value;
                if (_buttonDel != null) _buttonDel.BackColor = value;
                if (_buttonWrite != null) _buttonWrite.BackColor = value;

                if (_sp?.ColorScheme != null) _sp.ColorScheme.ButtonsColor = value;

                UpdateColorScheme();
            }
        }
        #endregion

        #region Constructor

        public TableControl() : base(true)
        {
            InitializeComponent();
        }

        protected override void OnFBLinkChanged()
        {
            base.OnFBLinkChanged();
            if (FBConnector.Fb != null)
            {
                InitializeServicesAndEvents();
            }
        }

        private void InitializeServicesAndEvents()
        {
            var fb = FBConnector.Fb as MbeTableFB ?? throw new NullReferenceException(
                "No connection between MbeTableFB and TableControl was  established");

            _sp = fb.ServiceProvider;

            _recipeViewModel = _sp.RecipeViewModel;
            _tableSchema = _sp.TableSchema;
            _statusManager = _sp.StatusManager;
            _tablePainter = _sp.TablePainter;
            _tableCellFormatter = _sp.TableCellFormatter;
            _dataProvider = _sp.DataProvider;
            _plcStateMonitor = _sp.PlcStateMonitor;
            _openFileDialog = _sp.OpenFileDialog;
            _saveFileDialog = _sp.SaveFileDialog;

            InitializeUi();
        }

        private void InitializeUi()
        {
            var colorScheme = GetColorSchemeFromProperties();
            var tableColumnManager = new TableColumnManager(_table, _tableSchema, colorScheme, _dataProvider);

            tableColumnManager.InitializeHeaders();
            tableColumnManager.InitializeTableColumns();

            _table.DataSource = _recipeViewModel.ViewModels;
            _table.Invalidate();

            _table.CellFormatting -= Table_CellFormatting;
            _table.CellFormatting += Table_CellFormatting;
            _table.CellBeginEdit -= Table_CellBeginEdit;
            _table.CellBeginEdit += Table_CellBeginEdit;
            _table.DataError -= Table_DataError;
            _table.DataError += Table_DataError;

            _statusManager.StatusUpdated -= OnStatusUpdated;
            _statusManager.StatusUpdated += OnStatusUpdated;
            _statusManager.StatusCleared -= OnStatusCleared;
            _statusManager.StatusCleared += OnStatusCleared;
        }

        #endregion

        #region Visuals
        private void UpdateColorScheme()
        {
            if (_colorScheme == null) return;

            _colorScheme.ButtonsColor = _buttonsColor;
            _colorScheme.HeaderFont = _headerFont;
            _colorScheme.LineBgColor = _lineBgColor;
            _colorScheme.LineFont = _lineFont;
            _colorScheme.LineTextColor = _lineTextColor;
            _colorScheme.HeaderBgColor = _headerBgColor;
            _colorScheme.HeaderTextColor = _headerTextColor;
            _colorScheme.SelectedLineFont = _selectedLineFont;
            _colorScheme.SelectedLineTextColor = _selectedLineTextColor;
            _colorScheme.SelectedLineBgColor = _selectedLineBgColor;
            _colorScheme.PassedLineFont = _passedLineFont;
            _colorScheme.PassedLineTextColor = _passedLineTextColor;
            _colorScheme.PassedLineBgColor = _passedLineBgColor;
            _colorScheme.ControlBackgroundColor = _controlBgColor;
            _colorScheme.TableBackgroundColor = _tableBgColor;
        }

        private ColorScheme GetColorSchemeFromProperties()
        {
            return new ColorScheme
            {
                ControlBackgroundColor = this.ControlBgColor,
                HeaderFont = this.HeaderFont,
                HeaderTextColor = this.HeaderTextColor,
                HeaderBgColor = this.HeaderBgColor,
                LineFont = this.LineFont,
                LineTextColor = this.LineTextColor,
                LineBgColor = this.LineBgColor,
                SelectedLineFont = this.SelectedLineFont,
                SelectedLineTextColor = this.SelectedLineTextColor,
                SelectedLineBgColor = this.SelectedLineBgColor,
                PassedLineFont = this.PassedLineFont,
                PassedLineTextColor = this.PassedLineTextColor,
                PassedLineBgColor = this.PassedLineBgColor,
                ButtonsColor = this.ButtonsColor,
                TableBackgroundColor = this.TableBgColor
            };
        }
        #endregion

        private void OnStatusUpdated(string message, StatusMessage statusMessage)
        {
            _labelStatus.Text = message;
            _labelStatus.BackColor = statusMessage == StatusMessage.Error ? Color.OrangeRed : _controlBgColor;
        }

        private void OnStatusCleared()
        {
            _labelStatus.Text = string.Empty;
            _labelStatus.BackColor = _controlBgColor;
        }

        private void Table_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            _sp?.StatusManager.WriteStatusMessage($"DataError in [{e.RowIndex}, {e.ColumnIndex}]: {e.Exception.Message}", StatusMessage.Error);
            e.ThrowException = true;
        }

        private void Table_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_sp == null) return;
            
            var rowIndex = e.RowIndex;
            var columnIndex = e.ColumnIndex;

            var currentRow = _plcStateMonitor.CurrentLineNumber;

            // var viewModel = _recipeViewModel.ViewModels[e.RowIndex];
            // var columnKey = _tableSchema.GetColumnKeyByIndex(e.ColumnIndex);
            // var columnDef = _tableSchema.GetColumnDefinition(e.ColumnIndex);
            //
            // var stateType = _tablePainter.GetStateType(viewModel, actualLineNumber, columnKey);
            //
            // _tablePainter.ApplyState(e.CellStyle, stateType);
            //
            // if (_table[e.ColumnIndex, e.RowIndex] is DataGridViewComboBoxCell cell)
            // {
            //     if (!columnDef.ComboBoxSource.IsStatic)
            //     {
            //         var dynamicSource = _dataProvider.GetActionTargetDatasource(viewModel);
            //         if (cell.DataSource != dynamicSource)
            //         {
            //             cell.DataSource = dynamicSource;
            //         }
            //         
            //     }
            // }
            // else
            // {
            //     if (stateType != TablePainter.StateType.Blocked)
            //     {
            //         e.Value = _tableCellFormatter.GetFormattedValue(viewModel, columnKey);
            //     }
            // }
        }

        private void Table_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (_sp == null || e.RowIndex < 0) return;

            var columnDef = _tableSchema.GetColumnDefinition(e.ColumnIndex);
            if (columnDef.TableCellType == typeof(DataGridViewComboBoxCell) && !columnDef.ComboBoxSource.IsStatic)
            {
                var viewModel = _recipeViewModel.ViewModels[e.RowIndex];
                if (_table[e.ColumnIndex, e.RowIndex] is DataGridViewComboBoxCell cell)
                {
                    var dynamicSource = _dataProvider.GetActionTargetDatasource(viewModel);
                    cell.DataSource = new BindingSource(dynamicSource, null);
                }
            }
        }

        #region Обработчики нажатия кнопок
        private void ClickButton_Delete(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _sp == null) return;
            var selectedRowIndex = _table.CurrentRow?.Index ?? -1;
            if (selectedRowIndex < 0) return;

            if (!_recipeViewModel.RemoveStep(selectedRowIndex, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
            }
        }

        private void ClickButton_AddLineBefore(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _sp == null) return;
            var selectedRowIndex = _table.CurrentRow?.Index ?? 0;

            if (!_recipeViewModel.AddNewStep(selectedRowIndex, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
            }
        }

        private void ClickButton_AddLineAfter(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _sp == null) return;
            var selectedRowIndex = _table.CurrentRow?.Index ?? 0;

            if (!_recipeViewModel.AddNewStep(selectedRowIndex + 1, out var errorString))
            {
                _statusManager.WriteStatusMessage(errorString, StatusMessage.Error);
            }
        }

        private void ClickButton_Open(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _sp == null || _openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            // _fileManager.LoadRecipe(sp.OpenFileDialog.FileName);
        }

        private void ClickButton_Save(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode || _sp == null || _saveFileDialog.ShowDialog() != DialogResult.OK)
                return;
            // _fileManager.SaveRecipe(sp.SaveFileDialog.FileName);
        }
        #endregion
    }
}