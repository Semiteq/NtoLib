using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Presentation;
using NtoLib.Recipes.MbeTable.Presentation.Adapters;
using NtoLib.Recipes.MbeTable.Presentation.Behavior;
using NtoLib.Recipes.MbeTable.Presentation.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Initialization;
using NtoLib.Recipes.MbeTable.Presentation.Style;
using NtoLib.Recipes.MbeTable.Journaling.Status;
using NtoLib.Recipes.MbeTable.Application.Services;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Abstractions;
using NtoLib.Recipes.MbeTable.Presentation.Commands;
using NtoLib.Recipes.MbeTable.Presentation.Rendering;
using NtoLib.Recipes.MbeTable.Presentation.State;
using NtoLib.Recipes.MbeTable.Presentation.StateProviders;

namespace NtoLib.Recipes.MbeTable;

public partial class TableControl
{
    private void InitializeServicesAndRuntime()
    {
        if (_runtimeInitialized) return;
        if (FBConnector.Fb is not MbeTableFB mbeTableFb) return;

        _serviceProvider = mbeTableFb.ServiceProvider;
        if (_serviceProvider is null) return;

        try
        {
            InitializeLogger();
            SubscribeGlobalServices();
            InitializeColorScheme();

            ConfigureRecipeTable();
            AttachBehaviorManager();
            InitializeRenderCoordinatorInternal();

            InitializePresenter();

            MarkInitialized();
            TryReadFromPlc();
            ApplyInitialPermissions();
        }
        catch (Exception ex)
        {
            HandleInitializationError(ex);
        }
    }

    private void InitializeLogger()
    {
        _logger = _serviceProvider!.GetRequiredService<ILogger>();
        _logger.LogDebug("Starting TableControl initialization");
    }

    private void SubscribeGlobalServices()
    {
        var statusManager = _serviceProvider!.GetRequiredService<IStatusManager>();
        statusManager.StatusUpdated += OnStatusUpdated;
        statusManager.StatusCleared += OnStatusCleared;

        var uiStateService = _serviceProvider!.GetRequiredService<IUiStateService>();
        uiStateService.StatusMessagePosted += OnUiStatusMessage;
        uiStateService.PermissionsChanged += OnPermissionsChanged;
    }

    private void InitializeColorScheme()
    {
        _colorSchemeProvider = _serviceProvider!.GetRequiredService<IColorSchemeProvider>()
            as DesignTimeColorSchemeProvider;

        ApplyPendingColorMutations();
    }

    private void ConfigureRecipeTable()
    {
        var configurator = new TableConfigurator(
            _table,
            _serviceProvider!.GetRequiredService<IColorSchemeProvider>(),
            _serviceProvider!.GetRequiredService<IReadOnlyList<ColumnDefinition>>(),
            _serviceProvider!.GetRequiredService<IColumnFactoryRegistry>());

        configurator.Configure();
    }

    private void AttachBehaviorManager()
    {
        _behaviorManager = ActivatorUtilities.CreateInstance<TableBehaviorManager>(_serviceProvider!, _table);
        _behaviorManager.Attach();
    }

    private void InitializeRenderCoordinatorInternal()
    {
        _renderCoordinator = ActivatorUtilities.CreateInstance<TableRenderCoordinator>(
            _serviceProvider!,
            _table,
            _serviceProvider!.GetRequiredService<IRowExecutionStateProvider>(),
            _serviceProvider!.GetRequiredService<ICellStateResolver>(),
            _serviceProvider!.GetRequiredService<RecipeViewModel>(),
            _serviceProvider!.GetRequiredService<IReadOnlyList<ColumnDefinition>>(),
            _serviceProvider!.GetRequiredService<ILogger>(),
            _serviceProvider!.GetRequiredService<IColorSchemeProvider>());

        _renderCoordinator.Initialize();
    }

    private void InitializePresenter()
    {
        _tableView = new DataGridViewAdapter(_table);
        _presenter = CreatePresenter(_tableView);
        _presenter.Initialize();
    }

    private void MarkInitialized()
    {
        _runtimeInitialized = true;
        _logger!.LogDebug("TableControl initialization completed");
    }

    private void TryReadFromPlc()
    {
        _logger!.LogDebug("Trying initial reading recipe from PLC");
        if (_presenter == null) return;

        if (IsHandleCreated)
        {
            BeginInvoke(new Action(async () =>
            {
                try
                {
                    await _presenter.ReceiveRecipeAsync().ConfigureAwait(true);
                }
                catch { }
            }));
        }
        else
        {
            _ = _presenter.ReceiveRecipeAsync();
        }
    }
    
    private void ApplyInitialPermissions()
    {
        if (!IsHandleCreated) return;
        try
        {
            _logger!.LogDebug("Applying initial permissions");
            var uiState = _serviceProvider?.GetService<IUiStateService>();
            if (uiState == null) return;

            var permissions = uiState.GetCurrentPermissions();
            _logger!.LogDebug("Current permissions: {Permissions}", permissions);
            
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnPermissionsChanged(permissions)));
            }
            else
            {
                OnPermissionsChanged(permissions);
            }
        }
        catch { /* ignored */ }
    }

    private void HandleInitializationError(Exception ex)
    {
        var logger = _serviceProvider?.GetService<ILogger>();
        logger?.LogCritical(ex, "TableControl initialization failed");

        MessageBox.Show(
            $@"Failed to initialize table: {ex.Message}",
            @"Initialization Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        throw ex;
    }

    private void OnStatusUpdated(string message, StatusMessage status)
    {
        if (_labelStatus.InvokeRequired)
        {
            _labelStatus.BeginInvoke(new Action(() => OnStatusUpdated(message, status)));
            return;
        }

        _labelStatus.Text = message;
        _labelStatus.BackColor = status switch
        {
            StatusMessage.Error => Color.LightCoral,
            StatusMessage.Warning => Color.LightYellow,
            StatusMessage.Success => Color.LightGreen,
            _ => _statusBgColor ?? ColorScheme.Default.StatusBgColor
        };
    }

    private void OnStatusCleared()
    {
        if (_labelStatus.InvokeRequired)
        {
            _labelStatus.BeginInvoke(new Action(OnStatusCleared));
            return;
        }

        _labelStatus.Text = string.Empty;
        _labelStatus.BackColor = _statusBgColor ?? ColorScheme.Default.StatusBgColor;
    }

    private void OnUiStatusMessage(string message, StatusKind kind)
    {
        if (_labelStatus.InvokeRequired)
        {
            _labelStatus.BeginInvoke(new Action(() => OnUiStatusMessage(message, kind)));
            return;
        }

        _labelStatus.Text = message;
        _labelStatus.BackColor = kind switch
        {
            StatusKind.Error => Color.LightCoral,
            StatusKind.Warning => Color.LightYellow,
            StatusKind.Info => Color.LightGreen,
            _ => _statusBgColor ?? ColorScheme.Default.StatusBgColor
        };
    }

    private void OnPermissionsChanged(UiPermissions permissions)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnPermissionsChanged(permissions)));
            return;
        }

        _buttonOpen.Enabled = permissions.CanOpenFile;
        _buttonSave.Enabled = permissions.CanSaveFile;
        _buttonAddBefore.Enabled = permissions.CanAddStep;
        _buttonAddAfter.Enabled = permissions.CanAddStep;
        _buttonDel.Enabled = permissions.CanDeleteStep;
        _buttonWrite.Enabled = permissions.CanWriteRecipe;
        _logger!.LogDebug("Permissions changed: {Permissions}", permissions);
    }

    private ITablePresenter CreatePresenter(ITableView view)
    {
        var app = _serviceProvider!.GetRequiredService<Application.IRecipeApplicationService>();
        var rowStateProvider = _serviceProvider!.GetRequiredService<IRowExecutionStateProvider>();
        var loadCmd = _serviceProvider!.GetRequiredService<LoadRecipeCommand>();
        var saveCmd = _serviceProvider!.GetRequiredService<SaveRecipeCommand>();
        var sendCmd = _serviceProvider!.GetRequiredService<SendRecipeCommand>();
        var receiveCmd = _serviceProvider!.GetRequiredService<ReceiveRecipeCommand>();
        var addCmd = _serviceProvider!.GetRequiredService<AddStepCommand>();
        var removeCmd = _serviceProvider!.GetRequiredService<RemoveStepCommand>();
        _logger = _serviceProvider!.GetRequiredService<ILogger>();

        return new TablePresenter(
            view,
            app,
            rowStateProvider,
            loadCmd,
            saveCmd,
            sendCmd,
            receiveCmd,
            addCmd,
            removeCmd);
    }

    public override void put_DesignMode(int bDesignMode)
    {
        base.put_DesignMode(bDesignMode);
        if (FBConnector == null) return;

        if (!FBConnector.DesignMode)
        {
            InitializeServicesAndRuntime();
        }
        else
        {
            CleanupRuntimeState();
        }
    }

    protected override void OnFBLinkChanged()
    {
        base.OnFBLinkChanged();

        try
        {
            if (FBConnector?.Fb != null && !FBConnector.DesignMode)
            {
                InitializeServicesAndRuntime();
            }
        }
        catch (ArgumentException ex) when (ex.Message.Contains("child with Name not found"))
        {
            MessageBox.Show(
                @"Failed to link UI to FB logic. FB in object tree does not match FB on mnemoscheme.",
                @"Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void CleanupRuntimeState()
    {
        try
        {
            if (_serviceProvider == null) return;

            UnsubscribeGlobalServices();
            DisposeRuntimeComponents();
        }
        catch
        {
        }

        ResetRuntimeFields();
    }

    private void UnsubscribeGlobalServices()
    {
        var statusManager = _serviceProvider!.GetService<IStatusManager>();
        if (statusManager != null)
        {
            statusManager.StatusUpdated -= OnStatusUpdated;
            statusManager.StatusCleared -= OnStatusCleared;
        }

        var uiStateService = _serviceProvider!.GetService<IUiStateService>();
        if (uiStateService != null)
        {
            uiStateService.StatusMessagePosted -= OnUiStatusMessage;
            uiStateService.PermissionsChanged -= OnPermissionsChanged;
        }
    }

    private void DisposeRuntimeComponents()
    {
        _presenter?.Dispose();
        (_tableView as IDisposable)?.Dispose();
        _behaviorManager?.Dispose();
        _behaviorManager = null;
    }

    private void ResetRuntimeFields()
    {
        _presenter = null;
        _tableView = null;
        _serviceProvider = null;
        _colorSchemeProvider = null;
        _runtimeInitialized = false;
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (!disposing) return;

            CleanupRuntimeState();
            components?.Dispose();

            TryDisposeFont(_headerFont);
            TryDisposeFont(_lineFont);
            TryDisposeFont(_selectedLineFont);
            TryDisposeFont(_passedLineFont);
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    private static void TryDisposeFont(Font? font)
    {
        try { font?.Dispose(); } catch { /* ignored */ }
    }
}