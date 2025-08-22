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
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
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
    [NonSerialized] private ServiceProvider _sp;

    [NonSerialized] private RecipeViewModel _recipeViewModel;
    [NonSerialized] private TableSchema _tableSchema;
    [NonSerialized] private IStatusManager _statusManager;
    [NonSerialized] private OpenFileDialog _openFileDialog;
    [NonSerialized] private SaveFileDialog _saveFileDialog;
    [NonSerialized] private ColorScheme _colorScheme;
    [NonSerialized] private IPlcStateMonitor _plcStateMonitor;
    [NonSerialized] private IActionTargetProvider _actionTargetProvider;
    [NonSerialized] private IComboboxDataProvider _comboboxDataProvider;
    [NonSerialized] private TableColumnFactoryMap _tableColumnFactoryMap;
    [NonSerialized] private TableCellStateManager _tableCellStateManager;
    [NonSerialized] private TableBehaviorManager _tableBehaviorManager;
    [NonSerialized] private IPlcRecipeStatusProvider _plcRecipeStatusProvider;
    [NonSerialized] private DebugLogger _debugLogger;
    [NonSerialized] private AppStateMachine _appStateMachine;
    [NonSerialized] private AppStateUiProjector _uiProjector;
    [NonSerialized] private ButtonStateStyler _buttonStyler;

    [NonSerialized] private readonly List<IDisposable> _eventSubscriptions = new();

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

    public override void put_DesignMode(int bDesignMode)
    {
        base.put_DesignMode(bDesignMode);
        AppCommand cmd = FBConnector.DesignMode ? new EnterEditor() : new EnterRuntime();
        _appStateMachine?.Dispatch(cmd);
    }

    #region Properties
    // Properties are initialized before OnFBLinkChanged
    // Null check is mandatory

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

    #region Constructor / lifecycle

    public TableControl() : base(true)
    {
        InitializeComponent();
        // Initializing only by OnFBLinkChanged, so the DI is passed by MbeTableFB
    }

    protected override void OnFBLinkChanged()
    {
        base.OnFBLinkChanged();
        if (FBConnector.Fb != null)
            InitializeServicesAndEvents();
    }

    private void InitializeServicesAndEvents()
    {
        var fb = FBConnector.Fb as MbeTableFB ?? throw new NullReferenceException(
            "No connection between MbeTableFB and TableControl was  established");

        _sp = fb.ServiceProvider;

        // MbeTableFB is not initialized in DesignMode.
        // In this case skipping table initialization.
        if (!_sp.IsInitialized) return;

        _sp.AttachUiDispatcher(new WinFormsUiDispatcher(this));

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

        _actionTargetProvider?.RefreshTargets();

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
        Subscribe(() => _appStateMachine.StateChanged += OnAppStateChanged,
            () => _appStateMachine.StateChanged -= OnAppStateChanged);

        Subscribe(() => _plcRecipeStatusProvider.AvailabilityChanged += OnPlcAvailabilityChanged,
            () => _plcRecipeStatusProvider.AvailabilityChanged -= OnPlcAvailabilityChanged);

        try
        {
            var initialAvailability = _plcRecipeStatusProvider.GetAvailability();
            _appStateMachine.Dispatch(new PlcAvailabilityChanged(initialAvailability));
        }
        catch
        {
            _appStateMachine.Dispatch(new PlcAvailabilityChanged(new PlcRecipeAvailable(false, false)));
        }

        _appStateMachine.Dispatch(FBConnector.DesignMode ? new EnterEditor() : new EnterRuntime());

    }

    private void OnVmUpdateStart() => _table.SuspendLayout();
    private void OnVmUpdateEnd() => _table.ResumeLayout();

    private void OnPlcAvailabilityChanged(PlcRecipeAvailable avail)
        => _appStateMachine.Dispatch(new PlcAvailabilityChanged(avail));

    private void OnStatusUpdated(string message, StatusMessage statusMessage)
    {
        _labelStatus.Text = message;
        _labelStatus.BackColor = statusMessage switch
        {
            StatusMessage.Error => Color.OrangeRed,
            StatusMessage.Success => Color.DarkSeaGreen,
            StatusMessage.Info => Color.AliceBlue,
            _ => _controlBgColor,
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearSubscriptions();

            try { _buttonStyler?.Dispose(); } catch { /* ignore */ }
            try { _tableBehaviorManager?.Dispose(); } catch { /* ignore */ }
            try { components?.Dispose(); } catch { /* ignore */ }

            TryDisposeFont(_headerFont);
            TryDisposeFont(_lineFont);
            TryDisposeFont(_selectedLineFont);
            TryDisposeFont(_passedLineFont);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region Visuals

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
        // ApplySchemeToUi();
    }

    private static Color Darken(Color c)
    {
        const int d = 40;
        int clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);
        return Color.FromArgb(c.A, clamp(c.R - d), clamp(c.G - d), clamp(c.B - d));
    }

    private void ApplySchemeToUi()
    {
        if (_table == null || _colorScheme == null) return;
        _table.ColumnHeadersDefaultCellStyle.Font = _colorScheme.HeaderFont;
        _table.RowHeadersDefaultCellStyle.Font = _colorScheme.HeaderFont;
        _table.BackgroundColor = _colorScheme.TableBackgroundColor;
        _table.RowTemplate.Height = _colorScheme.LineHeight;
        foreach (DataGridViewColumn c in _table.Columns)
            c.DefaultCellStyle.Font = _colorScheme.LineFont;
        _table.Invalidate();
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

    #endregion

    private void Table_DataError(object sender, DataGridViewDataErrorEventArgs e)
    {
        if (e.Cancel) return;
        _sp.StatusManager.WriteStatusMessage(
            $"DataError in [{e.RowIndex}, {e.ColumnIndex}]: {e.Exception.Message}", StatusMessage.Error);
        e.ThrowException = false;
    }

    #region Button Click Handlers

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

    private void InitButtonStyling()
    {
        _buttonStyler?.Dispose();
        _buttonStyler = new ButtonStateStyler
        {
            DisabledBackColor = Color.FromArgb(70, 70, 70),
            DisabledForeColor = Color.Gainsboro
        };

        _buttonStyler.Register(_buttonOpen);
        _buttonStyler.Register(_buttonSave);
        _buttonStyler.Register(_buttonAddBefore);
        _buttonStyler.Register(_buttonAddAfter);
        _buttonStyler.Register(_buttonDel);
        _buttonStyler.Register(_buttonWrite);
    }

    #endregion

    /// <summary>
    /// Safely executes the specified action on the UI thread. Ensures that the action is executed
    /// on the correct thread in a thread-safe manner, suppressing any exceptions that may occur.
    /// </summary>
    /// <param name="action">The action to be executed on the UI thread.</param>
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
            /* ignore */
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
            try
            {
                _eventSubscriptions[i]?.Dispose();
            }
            catch
            {
                /* ignore */
            }
        }

        _eventSubscriptions.Clear();
    }

    private static void TryDisposeFont(Font f)
    {
        try { f?.Dispose(); } catch { /* ignore */ }
    }

    /// <summary>
    /// Represents an action that will be executed when the instance is disposed.
    /// </summary>
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
}
