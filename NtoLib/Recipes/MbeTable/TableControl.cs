#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Composition.StateMachine;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table.Behavior;
using NtoLib.Recipes.MbeTable.Presentation.Table.CellState;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable;

[ComVisible(true)]
[DisplayName("Таблица рецептов МБЕ")]
[Guid("8161DF32-8D80-4B81-AF52-3021AE0AD293")]
public partial class TableControl : VisualControlBase
{
    // DI and services
    [NonSerialized] private ServiceProvider _sp;
    [NonSerialized] private RecipeViewModel _recipeViewModel;
    [NonSerialized] private TableSchema _tableSchema;
    [NonSerialized] private IStatusManager _statusManager;
    [NonSerialized] private IPlcStateMonitor _plcStateMonitor;
    [NonSerialized] private IActionTargetProvider _actionTargetProvider;
    [NonSerialized] private IComboboxDataProvider _comboboxDataProvider;
    [NonSerialized] private TableColumnFactoryMap _tableColumnFactoryMap;
    [NonSerialized] private TableCellStateManager _tableCellStateManager;
    [NonSerialized] private IPlcRecipeStatusProvider _plcRecipeStatusProvider;
    [NonSerialized] private DebugLogger _debugLogger;
    [NonSerialized] private AppStateMachine _appStateMachine;
    [NonSerialized] private AppStateUiProjector _uiProjector;

    // UI artifacts
    [NonSerialized] private TableBehaviorManager _tableBehaviorManager;
    [NonSerialized] private ButtonStateStyler _buttonStyler;
    [NonSerialized] private OpenFileDialog _openFileDialog;
    [NonSerialized] private SaveFileDialog _saveFileDialog;
    [NonSerialized] private ColorScheme _colorScheme;

    // Subscriptions container
    [NonSerialized] private readonly List<IDisposable> _eventSubscriptions = new();

    // Appearance (design-time props)
    [NonSerialized] private Color _controlBgColor = Color.Gray;
    [NonSerialized] private Color _tableBgColor = Color.Gray;
    [NonSerialized] private Font _headerFont = new("Arial", 14f, FontStyle.Bold);
    [NonSerialized] private Color _headerTextColor = Color.Black;
    [NonSerialized] private Color _headerBgColor = Color.LightGray;
    [NonSerialized] private Font _lineFont = new("Arial", 12f);
    [NonSerialized] private Color _lineTextColor = Color.Black;
    [NonSerialized] private Color _lineBgColor = Color.White;
    [NonSerialized] private Font _selectedLineFont = new("Arial", 12f);
    [NonSerialized] private Color _selectedLineTextColor = Color.Black;
    [NonSerialized] private Color _selectedLineBgColor = Color.Green;
    [NonSerialized] private Font _passedLineFont = new("Arial", 12f);
    [NonSerialized] private Color _passedLineTextColor = Color.DarkGray;
    [NonSerialized] private Color _passedLineBgColor = Color.Yellow;
    [NonSerialized] private Color _buttonsColor = Color.LightGray;
    [NonSerialized] private int _rowHeight = 40;
    [NonSerialized] private Color _statusBgColor = Color.LightGray;

    public TableControl() : base(true)
    {
        InitializeComponent();
        // DI setup happens in OnFBLinkChanged
    }

    public override void put_DesignMode(int bDesignMode)
    {
        base.put_DesignMode(bDesignMode);
        // Idempotent mode switch (reducer keeps state)
        AppCommand cmd = FBConnector.DesignMode ? new EnterEditor() : new EnterRuntime();
        _appStateMachine?.Dispatch(cmd);
    }

    #region Design-time properties

    [DisplayName("Цвет фона")]
    public Color ControlBgColor
    {
        get => _controlBgColor;
        set
        {
            if (_controlBgColor == value) return;
            _controlBgColor = value;
            BackColor = value;
            UpdateColorScheme();
        }
    }

    [DisplayName("Цвет статуса")]
    public Color StatusBgColor
    {
        get => _statusBgColor;
        set
        {
            if (_statusBgColor == value) return;
            _statusBgColor = value;
            if (_labelStatus != null) _labelStatus.BackColor = value;
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
            UpdateColorScheme();
        }
    }

    [DisplayName("Цвет текста пройденной строки таблицы")]
    public Color PassedLineTextColor
    {
        get => _passedLineTextColor;
        set
        {
            if (_passedLineTextColor == value) return;
            _passedLineTextColor = value;
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
            UpdateColorScheme();
        }
    }

    [DisplayName("Высота строки")]
    public int RowLineHeight
    {
        get => _rowHeight;
        set
        {
            if (_rowHeight == value) return;
            _rowHeight = value;
            if (_table != null) _table.RowTemplate.Height = value;
            UpdateColorScheme();
        }
    }

    #endregion

    #region Lifecycle

    protected override void OnFBLinkChanged()
    {
        base.OnFBLinkChanged();

        try
        {
            if (FBConnector.Fb != null)
                InitializeServicesAndEvents();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Не могу найти child с Name"))
        {
            MessageBox.Show(
                "Не удалось установить связь блока интерфейса с блоком логики ФБ. " +
                "Нужно целиком удалить ФБ из дерева и добавить заново для реинициализации в SCADA.",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            throw;
        }
    }

    private void InitializeServicesAndEvents()
    {
        var fb = FBConnector.Fb as MbeTableFB
                 ?? throw new NullReferenceException("No connection between MbeTableFB and TableControl was established");

        _sp = fb.ServiceProvider;

        // Some environments instantiate control in designer without initializing ServiceProvider
        if (!_sp.IsInitialized) return;

        // Attach UI dispatcher before any VM/UI work
        _sp.AttachUiDispatcher(new WinFormsUiDispatcher(this));

        // Resolve services
        _recipeViewModel = _sp.RecipeViewModel;
        _tableSchema = _sp.TableSchema;
        _statusManager = _sp.StatusManager;
        _plcStateMonitor = _sp.PlcStateMonitor;
        _openFileDialog = _sp.OpenFileDialog;
        _saveFileDialog = _sp.SaveFileDialog;
        _actionTargetProvider = _sp.ActionTargetProvider;
        _comboboxDataProvider = _sp.ComboboxDataProvider;
        _tableColumnFactoryMap = _sp.TableColumnFactoryMap;
        _tableCellStateManager = _sp.TableCellStateManager;
        _debugLogger = _sp.DebugLogger;
        _plcRecipeStatusProvider = _sp.PlcRecipeStatusProvider;
        _appStateMachine = _sp.AppStateMachine;
        _uiProjector = _sp.AppStateUiProjector;
        _colorScheme = _sp.ColorScheme;

        UpdateColorScheme();
        _actionTargetProvider.RefreshTargets();

        InitializeUi();
        InitializeAppState();
    }

    private void InitializeUi()
    {
        ClearSubscriptions();

        InitButtonStyling();
        EnableDoubleBufferDataGridView();

        var tableColumnManager = new TableColumnManager(_table, _tableSchema, _tableColumnFactoryMap.GetMap, _colorScheme);
        tableColumnManager.InitializeHeaders();
        tableColumnManager.InitializeTableColumns();
        tableColumnManager.InitializeTableRows();

        _table.RowHeadersWidth = 50;
        _table.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        _table.DataSource = _recipeViewModel.ViewModels;
        _table.Invalidate();

        _tableBehaviorManager = new TableBehaviorManager(_table, _tableSchema, _tableCellStateManager, _debugLogger);
        _tableBehaviorManager.TableStyleSetup();
        _tableBehaviorManager.Attach();

        // UI + VM subscriptions (auto-unsubscribe on Dispose)
        Subscribe(() => _table.DataError += Table_DataError,
                  () => _table.DataError -= Table_DataError);

        Subscribe(() => _recipeViewModel.OnUpdateStart += OnVmUpdateStart,
                  () => _recipeViewModel.OnUpdateStart -= OnVmUpdateStart);

        Subscribe(() => _recipeViewModel.OnUpdateEnd += OnVmUpdateEnd,
                  () => _recipeViewModel.OnUpdateEnd -= OnVmUpdateEnd);

        Subscribe(() => _statusManager.StatusUpdated += OnStatusUpdated,
                  () => _statusManager.StatusUpdated -= OnStatusUpdated);

        Subscribe(() => _statusManager.StatusCleared += OnStatusCleared,
                  () => _statusManager.StatusCleared -= OnStatusCleared);
    }

    private void InitializeAppState()
    {
        if (_appStateMachine == null) return;

        // State machine projection updates
        Subscribe(() => _appStateMachine.StateChanged += OnAppStateChanged,
                  () => _appStateMachine.StateChanged -= OnAppStateChanged);

        // PLC availability relay to state machine
        Action<PlcRecipeAvailable> handler = avail => _appStateMachine.Dispatch(new PlcAvailabilityChanged(avail));
        Subscribe(() => _plcRecipeStatusProvider.AvailabilityChanged += handler,
                  () => _plcRecipeStatusProvider.AvailabilityChanged -= handler);

        // Mode first, then initial availability (so PLC flags are not wiped)
        _appStateMachine.Dispatch(FBConnector.DesignMode ? new EnterEditor() : new EnterRuntime());

        try
        {
            var initial = _plcRecipeStatusProvider.GetAvailability();
            _appStateMachine.Dispatch(new PlcAvailabilityChanged(initial));
        }
        catch
        {
            _appStateMachine.Dispatch(new PlcAvailabilityChanged(new PlcRecipeAvailable(false, false)));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe all events safely
            ClearSubscriptions();

            // Dispose helpers and components
            try { _buttonStyler?.Dispose(); } catch { /* ignore */ }
            try { _tableBehaviorManager?.Dispose(); } catch { /* ignore */ }
            try { components?.Dispose(); } catch { /* ignore */ }

            // Fonts are owned by control when set from designer
            TryDisposeFont(_headerFont);
            TryDisposeFont(_lineFont);
            TryDisposeFont(_selectedLineFont);
            TryDisposeFont(_passedLineFont);
        }

        base.Dispose(disposing);
    }

    #endregion

    #region Event handlers

    private void OnVmUpdateStart() => _table.SuspendLayout();
    private void OnVmUpdateEnd() => _table.ResumeLayout();

    private void OnStatusUpdated(string message, StatusMessage statusMessage)
    {
        _labelStatus.Text = message;
        _labelStatus.BackColor = statusMessage switch
        {
            StatusMessage.Error => Color.OrangeRed,
            StatusMessage.Success => Color.DarkSeaGreen,
            StatusMessage.Info => Color.PaleTurquoise,
            _ => _statusBgColor
        };
    }

    private void OnStatusCleared()
    {
        _labelStatus.Text = string.Empty;
        _labelStatus.BackColor = _controlBgColor;
    }

    private void OnAppStateChanged(AppState s)
    {
        var p = _uiProjector.Project(s);
        SafeUi(() =>
        {
            _buttonWrite.Enabled = p.EnableWrite;
            _buttonOpen.Enabled = p.EnableOpen;
            _buttonAddAfter.Enabled = p.EnableAddAfter;
            _buttonAddBefore.Enabled = p.EnableAddBefore;
            _buttonDel.Enabled = p.EnableDelete;
            _buttonSave.Enabled = p.EnableSave;

            if (!string.IsNullOrEmpty(p.StatusText))
                _statusManager.WriteStatusMessage(p.StatusText!, p.StatusKind);
            else
                _statusManager.ClearStatusMessage();
        });
    }

    private void Table_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        if (e.Cancel) return;
        _sp.StatusManager.WriteStatusMessage(
            $"DataError in [{e.RowIndex}, {e.ColumnIndex}]: {e.Exception.Message}", StatusMessage.Error);
        e.ThrowException = false;
    }

    #endregion

    #region Button click handlers

    private void ClickButton_Delete(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode) return;
        var selectedRowIndex = _table.CurrentRow?.Index ?? -1;
        if (selectedRowIndex < 0) return;
        _recipeViewModel.RemoveStep(selectedRowIndex);
    }

    private void ClickButton_AddLineBefore(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode) return;
        var selectedRowIndex = _table.CurrentRow?.Index ?? 0;
        _recipeViewModel.AddNewStep(selectedRowIndex);
    }

    private void ClickButton_AddLineAfter(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode) return;
        var selectedRowIndex = _table.CurrentRow?.Index ?? 0;
        _recipeViewModel.AddNewStep(selectedRowIndex + 1);
    }

    private void ClickButton_Open(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _openFileDialog.ShowDialog() != DialogResult.OK) return;
        _appStateMachine?.Dispatch(new LoadRecipeRequested(_openFileDialog.FileName));
    }

    private void ClickButton_Save(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _saveFileDialog.ShowDialog() != DialogResult.OK) return;
        _appStateMachine?.Dispatch(new SaveRecipeRequested(_saveFileDialog.FileName));
    }

    private void ClickButton_Send(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode) return;
        _appStateMachine?.Dispatch(new SendRecipeRequested());
    }

    #endregion

    #region Visual helpers

    private void InitButtonStyling()
    {
        _buttonStyler?.Dispose();
        _buttonStyler = new ButtonStateStyler
        {
            DisabledBackColor = Color.FromArgb(170, 170, 170),
            DisabledForeColor = Color.Gainsboro
        };

        _buttonStyler.Register(_buttonOpen);
        _buttonStyler.Register(_buttonSave);
        _buttonStyler.Register(_buttonAddBefore);
        _buttonStyler.Register(_buttonAddAfter);
        _buttonStyler.Register(_buttonDel);
        _buttonStyler.Register(_buttonWrite);
    }

    private void EnableDoubleBufferDataGridView()
    {
        typeof(DataGridView).InvokeMember(
            "DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.SetProperty,
            null,
            _table,
            new object[] { true });
    }

    private void UpdateColorScheme()
    {
        if (_sp == null || !_sp.IsInitialized) return;

        var newScheme = new ColorScheme(
            ControlBackgroundColor: _controlBgColor,
            TableBackgroundColor: _tableBgColor,
            HeaderFont: _headerFont,
            LineFont: _lineFont,
            SelectedLineFont: _selectedLineFont,
            PassedLineFont: _passedLineFont,
            BlockedFont: _lineFont,
            HeaderTextColor: _headerTextColor,
            LineTextColor: _lineTextColor,
            SelectedLineTextColor: _selectedLineTextColor,
            PassedLineTextColor: _passedLineTextColor,
            BlockedTextColor: _passedLineTextColor,
            HeaderBgColor: _headerBgColor,
            LineBgColor: _lineBgColor,
            SelectedLineBgColor: _selectedLineBgColor,
            PassedLineBgColor: _passedLineBgColor,
            BlockedBgColor: _headerBgColor,
            ButtonsColor: _buttonsColor,
            BlockedButtonsColor: Darken(_buttonsColor),
            LineHeight: _rowHeight
        );

        _sp.SetColorScheme(newScheme);
        _colorScheme = newScheme;
    }

    private static Color Darken(Color c)
    {
        const int d = 40;
        int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);
        return Color.FromArgb(c.A, Clamp(c.R - d), Clamp(c.G - d), Clamp(c.B - d));
    }

    #endregion

    #region Infrastructure helpers

    private void SafeUi(Action action)
    {
        try
        {
            if (IsHandleCreated && InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }
        catch
        {
            // ignore
        }
    }

    private IDisposable Subscribe(Action attach, Action detach)
    {
        attach();
        var d = new DisposableAction(detach);
        _eventSubscriptions.Add(d);
        return d;
    }

    private void ClearSubscriptions()
    {
        if (_eventSubscriptions.Count == 0) return;

        for (int i = _eventSubscriptions.Count - 1; i >= 0; i--)
        {
            try { _eventSubscriptions[i]?.Dispose(); } catch { /* ignore */ }
        }
        _eventSubscriptions.Clear();
    }

    private static void TryDisposeFont(Font f)
    {
        try { f?.Dispose(); } catch { /* ignore */ }
    }

    private sealed class DisposableAction : IDisposable
    {
        private Action? _dispose;
        public DisposableAction(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            var d = _dispose;
            if (d == null) return;
            _dispose = null;
            d();
        }
    }

    #endregion
}