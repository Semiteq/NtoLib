using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.Services;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation;
using NtoLib.Recipes.MbeTable.ModulePresentation.Abstractions;
using NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;
using NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;
using NtoLib.Recipes.MbeTable.ModulePresentation.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Commands;
using NtoLib.Recipes.MbeTable.ModulePresentation.Initialization;
using NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;
using NtoLib.Recipes.MbeTable.ServiceStatus;

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
            InitializeColorScheme();
            SubscribeGlobalServices();

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
        _logger = _serviceProvider!.GetRequiredService<ILogger<TableControl>>();
        _logger.LogDebug("Starting TableControl initialization");
    }

    private void SubscribeGlobalServices()
    {
        var uiStateService = _serviceProvider!.GetRequiredService<IUiPermissionService>();
        uiStateService.PermissionsChanged += OnPermissionsChanged;

        var status = _serviceProvider!.GetRequiredService<IStatusService>();
        var scheme = _serviceProvider!.GetRequiredService<ColorScheme>();
        status.AttachLabel(_labelStatus, scheme);
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
            _serviceProvider!.GetRequiredService<FactoryColumnRegistry>());

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
            _table);

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
            var uiState = _serviceProvider?.GetService<IUiPermissionService>();
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
        catch { }
    }

    private void HandleInitializationError(Exception ex)
    {
        var logger = _serviceProvider?.GetService<ILogger<TableControl>>();
        logger?.LogCritical(ex, "TableControl initialization failed");

        MessageBox.Show(
            $@"Failed to initialize table: {ex.Message}",
            @"Initialization Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        throw ex;
    }

    private void OnPermissionsChanged(UiPermissions permissions)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnPermissionsChanged(permissions)));
            return;
        }

        var scheme = _serviceProvider?.GetService<ColorScheme>();
        if (scheme == null) return;

        ApplyButtonPermission(_buttonOpen, permissions.CanOpenFile, scheme);
        ApplyButtonPermission(_buttonSave, permissions.CanSaveFile, scheme);
        ApplyButtonPermission(_buttonAddBefore, permissions.CanAddStep, scheme);
        ApplyButtonPermission(_buttonAddAfter, permissions.CanAddStep, scheme);
        ApplyButtonPermission(_buttonDel, permissions.CanDeleteStep, scheme);
        ApplyButtonPermission(_buttonWrite, permissions.CanWriteRecipe, scheme);
    
        if (_table != null)
        {
            var makeReadOnly = permissions.IsGridReadOnly;
            if (_table.ReadOnly != makeReadOnly)
            {
                if (makeReadOnly)
                {
                    try { _table.EndEdit(); } catch { }
                }
                _table.ReadOnly = makeReadOnly;
            }
        }
    }

    private static void ApplyButtonPermission(Button button, bool enabled, ColorScheme scheme)
    {
        button.Enabled = enabled;
        button.BackColor = enabled ? scheme.ButtonsColor : scheme.BlockedButtonsColor;
    }

    private ITablePresenter CreatePresenter(ITableView view)
    {
        var app = _serviceProvider!.GetRequiredService<IRecipeApplicationService>();
        var rowStateProvider = _serviceProvider!.GetRequiredService<IRowExecutionStateProvider>();
        var loadCmd = _serviceProvider!.GetRequiredService<LoadRecipeCommand>();
        var saveCmd = _serviceProvider!.GetRequiredService<SaveRecipeCommand>();
        var sendCmd = _serviceProvider!.GetRequiredService<SendRecipeCommand>();
        var receiveCmd = _serviceProvider!.GetRequiredService<ReceiveRecipeCommand>();
        var addCmd = _serviceProvider!.GetRequiredService<AddStepCommand>();
        var removeCmd = _serviceProvider!.GetRequiredService<RemoveStepCommand>();

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
        var uiStateService = _serviceProvider!.GetService<IUiPermissionService>();
        if (uiStateService != null)
        {
            uiStateService.PermissionsChanged -= OnPermissionsChanged;
        }

        var status = _serviceProvider!.GetService<IStatusService>();
        try { status?.Detach(); } catch { }
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
        try { font?.Dispose(); } catch { }
    }
}