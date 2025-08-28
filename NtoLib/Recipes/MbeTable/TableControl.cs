#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FB.VisualFB;
using Microsoft.Extensions.DependencyInjection;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Composition.StateMachine;
using NtoLib.Recipes.MbeTable.Composition.StateMachine.App;
using NtoLib.Recipes.MbeTable.Composition.StateMachine.ThreadDispatcher;
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
    [NonSerialized] private IServiceProvider? _sp;
    [NonSerialized] private RecipeViewModel? _recipeViewModel;
    [NonSerialized] private TableSchema? _tableSchema;
    [NonSerialized] private IStatusManager? _statusManager;
    [NonSerialized] private IPlcStateMonitor? _plcStateMonitor;
    [NonSerialized] private IActionTargetProvider? _actionTargetProvider;
    [NonSerialized] private IComboboxDataProvider? _comboboxDataProvider;
    [NonSerialized] private TableColumnFactoryMap? _tableColumnFactoryMap;
    [NonSerialized] private TableCellStateManager? _tableCellStateManager;
    [NonSerialized] private IPlcRecipeStatusProvider? _plcRecipeStatusProvider;
    [NonSerialized] private ILogger? _debugLogger;
    [NonSerialized] private AppStateMachine? _appStateMachine;
    [NonSerialized] private AppStateUiProjector? _uiProjector;

    // UI artifacts
    [NonSerialized] private TableBehaviorManager? _tableBehaviorManager;
    [NonSerialized] private ButtonStateStyler? _buttonStyler;
    [NonSerialized] private OpenFileDialog? _openFileDialog;
    [NonSerialized] private SaveFileDialog? _saveFileDialog;
    [NonSerialized] private ColorScheme? _colorScheme;

    // Subscriptions container
    [NonSerialized] private readonly List<IDisposable> _eventSubscriptions = new();

    // Appearance (design-time props)
    [NonSerialized] private Color? _controlBgColor;
    [NonSerialized] private Color? _tableBgColor;
    [NonSerialized] private Font? _headerFont;
    [NonSerialized] private Color? _headerTextColor;
    [NonSerialized] private Color? _headerBgColor;
    [NonSerialized] private Font? _lineFont;
    [NonSerialized] private Color? _lineTextColor;
    [NonSerialized] private Color? _lineBgColor;
    [NonSerialized] private Font? _selectedLineFont;
    [NonSerialized] private Color? _selectedLineTextColor;
    [NonSerialized] private Color? _selectedLineBgColor;
    [NonSerialized] private Font? _passedLineFont;
    [NonSerialized] private Color? _passedLineTextColor;
    [NonSerialized] private Color? _passedLineBgColor;
    [NonSerialized] private Color? _buttonsColor;
    [NonSerialized] private int? _rowHeight;
    [NonSerialized] private Color? _statusBgColor;

    public TableControl() : base(true)
    {
        InitializeComponent();
    }

    public override void put_DesignMode(int bDesignMode)
    {
        base.put_DesignMode(bDesignMode);
        if (FBConnector == null) return;

        // This method is the primary controller for switching between modes.
        if (!FBConnector.DesignMode) // Entering Runtime
        {
            // The ServiceProvider is created by MbeTableFB. We just connect to it.
            InitializeServicesAndEvents();
            _appStateMachine?.Dispatch(new EnterRuntime());
        }
        else // Entering Design-Time
        {
            _appStateMachine?.Dispatch(new EnterEditor());
            CleanupRuntimeState();
        }
    }

    #region Design-time properties

    /// <summary>
    /// Gets or sets the background color of the control.
    /// If not set by the designer, the default value from ColorScheme is used.
    /// </summary>
    [DisplayName("Цвет фона")]
    public Color ControlBgColor
    {
        get => _controlBgColor ??= ColorScheme.Default.ControlBackgroundColor;
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
        get => _statusBgColor ??= ColorScheme.Default.ControlBackgroundColor;
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
        get => _tableBgColor ??= ColorScheme.Default.TableBackgroundColor;
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
        get => _headerFont ??= ColorScheme.Default.HeaderFont;
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
        get => _headerTextColor ??= ColorScheme.Default.HeaderTextColor;
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
        get => _headerBgColor ??= ColorScheme.Default.HeaderBgColor;
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
        get => _lineFont ??= ColorScheme.Default.LineFont;
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
        get => _lineTextColor ??= ColorScheme.Default.LineTextColor;
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
        get => _lineBgColor ??= ColorScheme.Default.LineBgColor;
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
        get => _selectedLineFont ??= ColorScheme.Default.SelectedLineFont;
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
        get => _selectedLineTextColor ??= ColorScheme.Default.SelectedLineTextColor;
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
        get => _selectedLineBgColor ??= ColorScheme.Default.SelectedLineBgColor;
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
        get => _passedLineFont ??= ColorScheme.Default.PassedLineFont;
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
        get => _passedLineTextColor ??= ColorScheme.Default.PassedLineTextColor;
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
        get => _passedLineBgColor ??= ColorScheme.Default.PassedLineBgColor;
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
        get => _buttonsColor ??= ColorScheme.Default.ButtonsColor;
        set
        {
            if (_buttonsColor == value) return;
            _buttonsColor = value;
            // Обновляем UI сразу
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
        get => _rowHeight ??= ColorScheme.Default.LineHeight;
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
        System.Diagnostics.Debug.WriteLine("!!! TableControl.OnFBLinkChanged called !!!");
        // Initialization logic is now moved to put_DesignMode to correctly
        // handle the transition to runtime. This method only establishes the link.
        try
        {
            // We can attempt to initialize here if we are already in runtime.
            if (FBConnector?.Fb != null && !FBConnector.DesignMode)
            {
                InitializeServicesAndEvents();
            }
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Не могу найти child с Name"))
        {
            MessageBox.Show(
                "Не удалось установить связь блока интерфейса с блоком логики ФБ. " +
                "ФБ в дереве объектов не соответствует ФБ на мнемосхеме.",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void InitializeServicesAndEvents()
    {
        // Guard against multiple initializations.
        if (_sp != null) return;

        if (FBConnector?.Fb is not MbeTableFB fb || fb.ServiceProvider is not IServiceProvider sp)
        {
            System.Diagnostics.Debug.WriteLine("!!! TableControl.InitializeServicesAndEvents: Skipped, ServiceProvider not ready. !!!");
            return;
        }

        _sp = sp;
        _debugLogger = _sp.GetRequiredService<ILogger>();
        _debugLogger.Log("TableControl: Initializing runtime services and UI.", nameof(InitializeServicesAndEvents));

        // Resolve services from the standard IServiceProvider.
        _recipeViewModel = _sp.GetRequiredService<RecipeViewModel>();
        _tableSchema = _sp.GetRequiredService<TableSchema>();
        _statusManager = _sp.GetRequiredService<IStatusManager>();
        _plcStateMonitor = _sp.GetRequiredService<IPlcStateMonitor>();
        _openFileDialog = _sp.GetRequiredService<OpenFileDialog>();
        _saveFileDialog = _sp.GetRequiredService<SaveFileDialog>();
        _actionTargetProvider = _sp.GetRequiredService<IActionTargetProvider>();
        _comboboxDataProvider = _sp.GetRequiredService<IComboboxDataProvider>();
        _tableColumnFactoryMap = _sp.GetRequiredService<TableColumnFactoryMap>();
        _tableCellStateManager = _sp.GetRequiredService<TableCellStateManager>();
        _plcRecipeStatusProvider = _sp.GetRequiredService<IPlcRecipeStatusProvider>();
        _appStateMachine = _sp.GetRequiredService<AppStateMachine>();
        _uiProjector = _sp.GetRequiredService<AppStateUiProjector>();

        // Attach UI dispatcher directly to consumers
        var uiDispatcher = new WinFormsUiDispatcher(this);
        _recipeViewModel.SetUiDispatcher(uiDispatcher);

        // The ColorScheme will be created and applied by UpdateColorScheme.
        // We can get an initial instance if needed, but the properties will overwrite it.
        _colorScheme = _sp.GetService<ColorScheme>() ?? new ColorScheme();

        UpdateColorScheme();
        _actionTargetProvider.RefreshTargets();

        InitializeUi();
        InitializeAppState();

        _debugLogger.Log("TableControl: Runtime services and UI initialized successfully.", nameof(InitializeServicesAndEvents));
    }

    private void InitializeUi()
    {
        ClearSubscriptions();

        InitButtonStyling();
        EnableDoubleBufferDataGridView();

        var tableColumnManager = new TableColumnManager(_table, _tableSchema!, _tableColumnFactoryMap!.GetMap, _colorScheme!);
        tableColumnManager.InitializeHeaders();
        tableColumnManager.InitializeTableColumns();
        tableColumnManager.InitializeTableRows();

        _table.RowHeadersWidth = 50;
        _table.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        _table.DataSource = _recipeViewModel!.ViewModels;
        _table.Invalidate();

        _tableBehaviorManager = new TableBehaviorManager(_table, _tableSchema!, _tableCellStateManager!, _debugLogger);
        _tableBehaviorManager.TableStyleSetup();
        _tableBehaviorManager.Attach();

        // UI + VM subscriptions (auto-unsubscribe on Dispose)
        Subscribe(() => _table.DataError += Table_DataError,
                  () => _table.DataError -= Table_DataError);

        Subscribe(() => _recipeViewModel!.OnUpdateStart += OnVmUpdateStart,
                  () => _recipeViewModel!.OnUpdateStart -= OnVmUpdateStart);

        Subscribe(() => _recipeViewModel!.OnUpdateEnd += OnVmUpdateEnd,
                  () => _recipeViewModel!.OnUpdateEnd -= OnVmUpdateEnd);

        Subscribe(() => _statusManager!.StatusUpdated += OnStatusUpdated,
                  () => _statusManager!.StatusUpdated -= OnStatusUpdated);

        Subscribe(() => _statusManager!.StatusCleared += OnStatusCleared,
                  () => _statusManager!.StatusCleared -= OnStatusCleared);
    }

    private void InitializeAppState()
    {
        if (_appStateMachine == null) return;

        // State machine projection updates
        Subscribe(() => _appStateMachine.StateChanged += OnAppStateChanged,
                  () => _appStateMachine.StateChanged -= OnAppStateChanged);

        // PLC availability relay to state machine
        Action<PlcRecipeAvailable> handler = avail => _appStateMachine.Dispatch(new PlcAvailabilityChanged(avail));
        Subscribe(() => _plcRecipeStatusProvider!.AvailabilityChanged += handler,
                  () => _plcRecipeStatusProvider!.AvailabilityChanged -= handler);

        // Initial availability check
        try
        {
            var initial = _plcRecipeStatusProvider!.GetAvailability();
            _appStateMachine.Dispatch(new PlcAvailabilityChanged(initial));
        }
        catch
        {
            _appStateMachine.Dispatch(new PlcAvailabilityChanged(new PlcRecipeAvailable(false, false)));
        }
    }

    protected override void Dispose(bool disposing)
    {
        System.Diagnostics.Debug.WriteLine($"!!! TableControl.Dispose(disposing={disposing}) called !!!");
        if (disposing)
        {
            CleanupRuntimeState();
            components?.Dispose();

            // Безопасно освобождаем шрифты, только если они были созданы
            TryDisposeFont(_headerFont);
            TryDisposeFont(_lineFont);
            TryDisposeFont(_selectedLineFont);
            TryDisposeFont(_passedLineFont);
        }
        base.Dispose(disposing);
    }

    private void CleanupRuntimeState()
    {
        _debugLogger?.Log("TableControl: Cleaning up runtime state and subscriptions.", nameof(CleanupRuntimeState));
        System.Diagnostics.Debug.WriteLine("!!! TableControl.CleanupRuntimeState called. !!!");

        // Unsubscribe all events safely
        ClearSubscriptions();

        // Dispose helpers
        try { _buttonStyler?.Dispose(); } catch { /* ignore */ }
        try { _tableBehaviorManager?.Dispose(); } catch { /* ignore */ }

        // Clear all service references
        _sp = null;
        _recipeViewModel = null;
        _tableSchema = null;
        _statusManager = null;
        _plcStateMonitor = null;
        _actionTargetProvider = null;
        _comboboxDataProvider = null;
        _tableColumnFactoryMap = null;
        _tableCellStateManager = null;
        _plcRecipeStatusProvider = null;
        _appStateMachine = null;
        _uiProjector = null;
        _openFileDialog = null;
        _saveFileDialog = null;
        _colorScheme = null;
        _tableBehaviorManager = null;
        _buttonStyler = null;

        // The logger is the last to go
        _debugLogger = null;
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
            _ => ColorScheme.Default.StatusBgColor
        };
    }

    private void OnStatusCleared()
    {
        _labelStatus.Text = string.Empty;
        _labelStatus.BackColor = ColorScheme.Default.ControlBackgroundColor;
    }

    private void OnAppStateChanged(AppState s)
    {
        if (_uiProjector == null || _statusManager == null) return;

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
        if (e.Cancel || _sp == null) return;
        _statusManager?.WriteStatusMessage(
            $"DataError in [{e.RowIndex}, {e.ColumnIndex}]: {e.Exception?.Message}", StatusMessage.Error);
        e.ThrowException = false;
    }

    #endregion

    #region Button click handlers

    private void ClickButton_Delete(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode) return;
        var selectedRowIndex = _table.CurrentRow?.Index ?? -1;
        if (selectedRowIndex < 0) return;
        _recipeViewModel?.RemoveStep(selectedRowIndex);
    }

    private void ClickButton_AddLineBefore(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode) return;
        var selectedRowIndex = _table.CurrentRow?.Index ?? 0;
        _recipeViewModel?.AddNewStep(selectedRowIndex);
    }

    private void ClickButton_AddLineAfter(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode) return;
        var selectedRowIndex = _table.CurrentRow?.Index ?? 0;
        _recipeViewModel?.AddNewStep(selectedRowIndex + 1);
    }

    private void ClickButton_Open(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _openFileDialog?.ShowDialog() != DialogResult.OK) return;
        _appStateMachine?.Dispatch(new LoadRecipeRequested(_openFileDialog.FileName));
    }

    private void ClickButton_Save(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _saveFileDialog?.ShowDialog() != DialogResult.OK) return;
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
        if (_sp == null) return;

        // Этот метод теперь просто читает публичные свойства.
        // Ему не важно, откуда взялось значение - из дизайнера или из defaults.
        // Геттеры свойств сами об этом позаботятся.
        var newScheme = new ColorScheme() with
        {
            ControlBackgroundColor = this.ControlBgColor,
            TableBackgroundColor = this.TableBgColor,
            HeaderFont = this.HeaderFont,
            LineFont = this.LineFont,
            SelectedLineFont = this.SelectedLineFont,
            PassedLineFont = this.PassedLineFont,
            BlockedFont = this.LineFont,
            HeaderTextColor = this.HeaderTextColor,
            LineTextColor = this.LineTextColor,
            SelectedLineTextColor = this.SelectedLineTextColor,
            PassedLineTextColor = this.PassedLineTextColor,
            BlockedTextColor = this.PassedLineTextColor,
            HeaderBgColor = this.HeaderBgColor,
            LineBgColor = this.LineBgColor,
            SelectedLineBgColor = this.SelectedLineBgColor,
            PassedLineBgColor = this.PassedLineBgColor,
            BlockedBgColor = this.HeaderBgColor,
            ButtonsColor = this.ButtonsColor,
            BlockedButtonsColor = Darken(this.ButtonsColor),
            LineHeight = this.RowLineHeight,
            StatusBgColor = this.StatusBgColor
        };

        _colorScheme = newScheme;

        // Получаем сервис, который использует схему, и обновляем его напрямую
        var tableCellStateManager = _sp.GetService<TableCellStateManager>();
        tableCellStateManager?.UpdateColorScheme(newScheme);
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
            if (IsHandleCreated && !IsDisposed && InvokeRequired)
                BeginInvoke(action);
            else if (!IsDisposed)
                action();
        }
        catch (ObjectDisposedException)
        {
            // ignore if control is disposed during async operation
        }
        catch
        {
            // ignore other potential exceptions during shutdown
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

        var logger = _debugLogger; // Capture logger instance
        logger?.Log($"TableControl: Clearing {_eventSubscriptions.Count} event subscriptions.", nameof(ClearSubscriptions));
        System.Diagnostics.Debug.WriteLine($"!!! TableControl.ClearSubscriptions: Clearing {_eventSubscriptions.Count} subs. !!!");


        for (int i = _eventSubscriptions.Count - 1; i >= 0; i--)
        {
            try { _eventSubscriptions[i]?.Dispose(); } catch { /* ignore */ }
        }
        _eventSubscriptions.Clear();
    }



    private static void TryDisposeFont(Font? f)
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