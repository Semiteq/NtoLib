#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FB.VisualFB;
using Microsoft.Extensions.DependencyInjection;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.DataSource;
using NtoLib.Recipes.MbeTable.Presentation.Extensions;
using NtoLib.Recipes.MbeTable.Presentation.Initialization;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table.Behavior;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;
using NtoLib.Recipes.MbeTable.Presentation.Table.Rendering;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;
using NtoLib.Recipes.MbeTable.Presentation.Table.VirtualMode;
using NtoLib.Recipes.MbeTable.StateMachine;
using NtoLib.Recipes.MbeTable.StateMachine.App;
using NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

namespace NtoLib.Recipes.MbeTable;

[ComVisible(true)]
[DisplayName("Таблица рецептов МБЕ")]
[Guid("8161DF32-8D80-4B81-AF52-3021AE0AD293")]
public partial class TableControl : VisualControlBase
{
    [NonSerialized] private IServiceProvider? _sp;
    [NonSerialized] private RecipeViewModel? _recipeViewModel;
    [NonSerialized] private TableColumns? _tableSchema;
    [NonSerialized] private PropertyDefinitionRegistry? _registry;
    [NonSerialized] private IStatusManager? _statusManager;
    [NonSerialized] private IActionTargetProvider? _actionTargetProvider;
    [NonSerialized] private IComboboxDataProvider? _comboboxDataProvider;
    [NonSerialized] private IPlcRecipeStatusProvider? _plcRecipeStatusProvider;
    [NonSerialized] private ILogger? _debugLogger;
    [NonSerialized] private AppStateMachine? _appStateMachine;
    [NonSerialized] private AppStateUiProjector? _uiProjector;

    [NonSerialized] private VirtualModeDataManager? _virtualModeManager;
    [NonSerialized] private ITableRenderCoordinator? _tableRenderCoordinator;
    [NonSerialized] private TableBehaviorManager? _tableBehaviorManager;
    [NonSerialized] private OpenFileDialog? _openFileDialog;
    [NonSerialized] private SaveFileDialog? _saveFileDialog;
    [NonSerialized] private IColorSchemeProvider? _colorSchemeProvider;
    [NonSerialized] private IComboBoxContext? _comboBoxContext;
    [NonSerialized] private IRowExecutionStateProvider? _rowExecutionStateProvider;

    [NonSerialized] private readonly List<IDisposable> _eventSubscriptions = new();

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
    [NonSerialized] private Color? _blockedBgColor;
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

        if (!FBConnector.DesignMode)
        {
            InitializeServicesAndEvents();
            _appStateMachine?.Dispatch(new EnterRuntime());
        }
        else
        {
            _appStateMachine?.Dispatch(new EnterEditor());
            CleanupRuntimeState();
        }
    }

    #region Design-time properties

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

    [DisplayName("Цвет фона заблокированной ячейки")]
    public Color BlockedBgColor
    {
        get => _blockedBgColor ??= Color.FromArgb(224, 224, 224);
        set
        {
            if (_blockedBgColor == value) return;
            _blockedBgColor = value;
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

        try
        {
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
        if (_sp != null) return;

        if (FBConnector?.Fb is not MbeTableFB fb || fb.ServiceProvider is not IServiceProvider sp)
        {
            Debug.WriteLine("!!! TableControl.InitializeServicesAndEvents: Skipped, ServiceProvider not ready. !!!");
            return;
        }

        _sp = sp;
        _debugLogger = _sp.GetRequiredService<ILogger>();
        _debugLogger.Log("Initializing runtime services and UI.");

        _recipeViewModel = _sp.GetRequiredService<RecipeViewModel>();
        _tableSchema = _sp.GetRequiredService<TableColumns>();
        _registry = _sp.GetRequiredService<PropertyDefinitionRegistry>();
        _statusManager = _sp.GetRequiredService<IStatusManager>();
        _openFileDialog = _sp.GetRequiredService<OpenFileDialog>();
        _saveFileDialog = _sp.GetRequiredService<SaveFileDialog>();
        _actionTargetProvider = _sp.GetRequiredService<IActionTargetProvider>();
        _comboboxDataProvider = _sp.GetRequiredService<IComboboxDataProvider>();
        _plcRecipeStatusProvider = _sp.GetRequiredService<IPlcRecipeStatusProvider>();
        _appStateMachine = _sp.GetRequiredService<AppStateMachine>();
        _uiProjector = _sp.GetRequiredService<AppStateUiProjector>();
        _colorSchemeProvider = _sp.GetRequiredService<IColorSchemeProvider>();
        _comboBoxContext = _sp.GetRequiredService<IComboBoxContext>();
        _rowExecutionStateProvider = _sp.GetRequiredService<IRowExecutionStateProvider>();

        var uiDispatcher = new WinFormsUiDispatcher(this);
        _recipeViewModel.SetUiDispatcher(uiDispatcher);
        UpdateColorScheme();
        _actionTargetProvider.RefreshTargets();

        InitializeUi();
        InitializeAppState();

        _debugLogger.Log("Runtime services and UI initialized successfully.");
    }

    private void InitializeUi()
    {
        ClearSubscriptions();

        InitButtonStyling();

        var tableInitializer = new TableInitializer(
            _table,
            _colorSchemeProvider!,
            _tableSchema!,
            _registry!,
            _comboboxDataProvider!,
            _comboBoxContext!,
            _debugLogger!);

        tableInitializer.InitializeTable();

        InitializeComboBoxCellStrategies();

        _table.VirtualMode = true;
        _table.RowCount = _recipeViewModel!.GetRowCount();
        _table.Invalidate();

        _virtualModeManager = new VirtualModeDataManager(
            _recipeViewModel!,
            _tableSchema!,
            _table,
            _debugLogger!);

        _table.CellValueNeeded += OnCellValueNeeded;
        _table.CellValuePushed += OnCellValuePushed;

        Subscribe(() => _recipeViewModel!.RowCountChanged += OnRowCountChanged,
                  () => _recipeViewModel!.RowCountChanged -= OnRowCountChanged);

        Subscribe(() => _recipeViewModel!.ValidationFailed += OnValidationFailed,
                  () => _recipeViewModel!.ValidationFailed -= OnValidationFailed);

        var rowExecutionStateProvider = _sp!.GetRequiredService<IRowExecutionStateProvider>();
        var cellStateResolver = _sp!.GetRequiredService<ICellStateResolver>();
        
        _tableRenderCoordinator = new TableRenderCoordinator(
            _table,
            rowExecutionStateProvider,
            cellStateResolver,
            _recipeViewModel!,
            _tableSchema!,
            _debugLogger!,
            _colorSchemeProvider!);

        _tableRenderCoordinator.Initialize();

        _tableBehaviorManager = new TableBehaviorManager(
            _table,
            _statusManager,
            _colorSchemeProvider!.Current,
            _debugLogger);

        _tableBehaviorManager.Attach();

        Subscribe(() => _statusManager!.StatusUpdated += OnStatusUpdated,
                  () => _statusManager!.StatusUpdated -= OnStatusUpdated);

        Subscribe(() => _statusManager!.StatusCleared += OnStatusCleared,
                  () => _statusManager!.StatusCleared -= OnStatusCleared);

        Subscribe(() => _recipeViewModel!.RowInvalidationRequested += OnRowInvalidationRequested,
            () => _recipeViewModel!.RowInvalidationRequested -= OnRowInvalidationRequested);
    }

    private void InitializeComboBoxCellStrategies()
    {
        var scheme = _colorSchemeProvider!.Current;
        
        foreach (DataGridViewColumn column in _table.Columns)
        {
            if (column.Tag is not Type strategyType)
            {
                continue;
            }

            IComboBoxDataSourceStrategy? strategy = null;

            if (strategyType == typeof(ColumnStaticDataSource))
            {
                strategy = new ColumnStaticDataSource();
            }
            else if (strategyType == typeof(RowDynamicDataSource))
            {
                strategy = new RowDynamicDataSource();
            }

            if (strategy == null)
            {
                continue;
            }

            if (column.CellTemplate is RecipeComboBoxCell templateCell)
            {
                templateCell.Initialize(_comboBoxContext!, strategy, scheme, _rowExecutionStateProvider!);
            }
        }
    }

    private void OnRowInvalidationRequested(int rowIndex)
    {
        SafeUi(() =>
        {
            _virtualModeManager!.InvalidateRow(rowIndex);
        });
    }

    private void InitializeAppState()
    {
        if (_appStateMachine == null) return;

        Subscribe(() => _appStateMachine.StateChanged += OnAppStateChanged,
                  () => _appStateMachine.StateChanged -= OnAppStateChanged);

        Action<PlcRecipeAvailable> handler = avail => _appStateMachine.Dispatch(new PlcAvailabilityChanged(avail));
        Subscribe(() => _plcRecipeStatusProvider!.AvailabilityChanged += handler,
                  () => _plcRecipeStatusProvider!.AvailabilityChanged -= handler);

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
        try
        {
            if (disposing)
            {
                CleanupRuntimeState();
                components?.Dispose();

                TryDisposeFont(_headerFont);
                TryDisposeFont(_lineFont);
                TryDisposeFont(_selectedLineFont);
                TryDisposeFont(_passedLineFont);
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    private void CleanupRuntimeState()
    {
        ClearSubscriptions();

        try { _tableBehaviorManager?.Dispose(); } catch { }
        try { _tableRenderCoordinator?.Dispose(); } catch { }

        _sp = null;
        _recipeViewModel = null;
        _tableSchema = null;
        _statusManager = null;
        _actionTargetProvider = null;
        _comboboxDataProvider = null;
        _plcRecipeStatusProvider = null;
        _appStateMachine = null;
        _uiProjector = null;
        _openFileDialog = null;
        _saveFileDialog = null;
        _colorSchemeProvider = null;
        _comboBoxContext = null;
        _tableBehaviorManager = null;
        _tableRenderCoordinator = null;
        _virtualModeManager = null;

        _debugLogger = null;
        
    }

    #endregion

    #region VirtualMode Event Handlers

    private void OnCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        e.Value = _virtualModeManager!.GetCellValue(e.RowIndex, e.ColumnIndex);
    }

    private void OnCellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
    {
        var currentCell = _table.CurrentCell;
        int currentRow = currentCell?.RowIndex ?? -1;
        int currentCol = currentCell?.ColumnIndex ?? -1;

        var result = _virtualModeManager!.SetCellValue(e.RowIndex, e.ColumnIndex, e.Value);

        if (result.IsFailed)
        {
            try
            {
                _table.CancelEdit();
                _table.EndEdit();
                _virtualModeManager.InvalidateCell(e.RowIndex, e.ColumnIndex);
            }
            catch { }
        }
    }

    private int _previousRowCount = 0;
    
    private void OnRowCountChanged(int newCount)
    {
        SafeUi(() =>
        {
            _virtualModeManager!.RefreshRowCount();
            _previousRowCount = newCount;
        });
    }

    private void OnValidationFailed(int rowIndex, string errorMessage)
    {
        SafeUi(() =>
        {
            _statusManager?.WriteStatusMessage(errorMessage, StatusMessage.Error);
            _virtualModeManager!.InvalidateRow(rowIndex);
        });
    }

    #endregion

    #region Event handlers

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

    #endregion

    #region Visual helpers

    private void InitButtonStyling()
    {
        var disabledBack = Color.FromArgb(170, 170, 170);
        var disabledFore = Color.Gainsboro;

        _buttonOpen.SetupDisabledStyle(disabledBack, disabledFore);
        _buttonSave.SetupDisabledStyle(disabledBack, disabledFore);
        _buttonAddBefore.SetupDisabledStyle(disabledBack, disabledFore);
        _buttonAddAfter.SetupDisabledStyle(disabledBack, disabledFore);
        _buttonDel.SetupDisabledStyle(disabledBack, disabledFore);
        _buttonWrite.SetupDisabledStyle(disabledBack, disabledFore);
    }

    private void UpdateColorScheme()
    {
        if (_colorSchemeProvider == null) return;

        var newScheme = new ColorScheme
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
            BlockedTextColor = this.LineTextColor,
            HeaderBgColor = this.HeaderBgColor,
            LineBgColor = this.LineBgColor,
            SelectedLineBgColor = this.SelectedLineBgColor,
            PassedLineBgColor = this.PassedLineBgColor,
            BlockedBgColor = this.BlockedBgColor,
            ButtonsColor = this.ButtonsColor,
            BlockedButtonsColor = Darken(this.ButtonsColor),
            LineHeight = this.RowLineHeight,
            StatusBgColor = this.StatusBgColor,
            SelectedOutlineColor = this.SelectedLineBgColor,
            SelectedOutlineThickness = 1
        };

        if (_colorSchemeProvider is DesignTimeColorSchemeProvider provider)
        {
            provider.Update(newScheme);
        }
    }

    private static Color Darken(Color c)
    {
        const int d = 40;
        int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);
        return Color.FromArgb(c.A, Clamp(c.R - d), Clamp(c.G - d), Clamp(c.B - d));
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
        catch (ObjectDisposedException) { }
        catch { }
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
            try { _eventSubscriptions[i]?.Dispose(); } catch { }
        }
        _eventSubscriptions.Clear();
    }

    private static void TryDisposeFont(Font? f)
    {
        try { f?.Dispose(); } catch { }
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