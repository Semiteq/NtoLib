using System;
using System.Drawing;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModulePresentation;
using NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;
using NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;
using NtoLib.Recipes.MbeTable.ModulePresentation.Initialization;
using NtoLib.Recipes.MbeTable.ModulePresentation.Input;
using NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;
using NtoLib.Recipes.MbeTable.Utilities;

namespace NtoLib.Recipes.MbeTable;

public partial class TableControl
{
	[NonSerialized] private Timer? _permissionsDebounceTimer;
	[NonSerialized] private UiPermissions? _pendingPermissions;

	[NonSerialized] private TableInputManager? _inputManager;
	[NonSerialized] private TableControlServices? _services;

	private void InitializeServicesAndRuntime()
	{
		if (_runtimeInitialized)
		{
			return;
		}

		if (FBConnector.Fb is not MbeTableFB mbeTableFb)
		{
			return;
		}

		_serviceProvider = mbeTableFb.ServiceProvider;
		if (_serviceProvider is null)
		{
			return;
		}

		try
		{
			_services = _serviceProvider.GetRequiredService<TableControlServices>();

			InitializeLogger();
			InitializeColorScheme();
			SubscribeGlobalServices();

			ConfigureRecipeTable();
			AttachBehaviorManager();
			InitializeRenderCoordinatorInternal();
			InitializeInputManager();

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
		_logger = _services!.Logger;
		_logger.LogDebug("Starting TableControl initialization");
	}

	private void SubscribeGlobalServices()
	{
		_services!.StateProvider.PermissionsChanged += OnPermissionsChanged;

		_permissionsDebounceTimer = new Timer { Interval = 80 };
		_permissionsDebounceTimer.Tick += PermissionsDebounceTimer_Tick;

		_services.StatusService.AttachLabel(_labelStatus, _services.ColorScheme);
	}

	private void InitializeColorScheme()
	{
		_colorSchemeProvider = _services!.ColorSchemeProvider;

		ApplyPendingColorMutations();
	}

	private void ConfigureRecipeTable()
	{
		GridOptions.Init(_table);
		GridStyle.Init(_table, _services!.ColorSchemeProvider.Current);
		GridColumns.Init(_table, _services.ColumnDefinitions, _services.ColumnRegistry);
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

		_renderCoordinator?.Initialize();
	}

	private void InitializeInputManager()
	{
		if (_serviceProvider == null
			|| _table == null
			|| _table.IsDisposed)
		{
			return;
		}

		_inputManager = ActivatorUtilities.CreateInstance<TableInputManager>(_serviceProvider, _table);

		_inputManager.Attach();
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
		if (_presenter == null)
		{
			return;
		}

		if (IsHandleCreated)
		{
			BeginInvoke(new Action(async void () =>
			{
				try
				{
					await _presenter.ReceiveRecipeAsync().ConfigureAwait(true);
				}
				catch
				{
					/* ignored */
				}
			}));
		}
		else
		{
			_ = _presenter.ReceiveRecipeAsync();
		}
	}

	private void ApplyInitialPermissions()
	{
		if (!IsHandleCreated)
		{
			return;
		}

		try
		{
			_logger!.LogDebug("Applying initial permissions");
			var permissions = _services?.StateProvider.GetUiPermissions();
			if (permissions == null)
			{
				return;
			}

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
		catch
		{
			/* ignored */
		}
	}

	private void HandleInitializationError(Exception ex)
	{
		_logger?.LogCritical(ex, "TableControl initialization failed");

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

		// Debounce: store latest, restart timer
		_pendingPermissions = permissions;
		if (_permissionsDebounceTimer != null)
		{
			_permissionsDebounceTimer.Stop();
			_permissionsDebounceTimer.Start();
		}
		else
		{
			// Fallback: apply immediately if timer is not available
			ApplyPermissionsNow(permissions);
		}
	}

	private void PermissionsDebounceTimer_Tick(object? sender, EventArgs e)
	{
		if (_permissionsDebounceTimer == null)
		{
			return;
		}

		_permissionsDebounceTimer.Stop();
		if (_pendingPermissions != null)
		{
			ApplyPermissionsNow(_pendingPermissions);
		}
	}

	private void ApplyPermissionsNow(UiPermissions permissions)
	{
		var scheme = _services?.ColorScheme;
		if (scheme == null)
		{
			return;
		}

		ApplyButtonPermission(_buttonOpen, permissions.CanOpenFile, scheme);
		ApplyButtonPermission(_buttonSave, permissions.CanSaveFile, scheme);
		ApplyButtonPermission(_buttonAddBefore, permissions.CanAddStep, scheme);
		ApplyButtonPermission(_buttonAddAfter, permissions.CanAddStep, scheme);
		ApplyButtonPermission(_buttonDel, permissions.CanDeleteStep, scheme);
		ApplyButtonPermission(_buttonWrite, permissions.CanSendRecipe, scheme);

		if (_table != null)
		{
			var makeReadOnly = permissions.IsGridReadOnly;
			if (_table.ReadOnly != makeReadOnly)
			{
				if (makeReadOnly)
				{
					SafeDisposal.RunAll(() => _table.EndEdit());
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

	private TablePresenter CreatePresenter(ITableView view)
	{
		return new TablePresenter(
			view,
			_services!.RecipeOperationService,
			_services.RowStateProvider,
			_services.BusyStateManager,
			_services.OpenFileDialog,
			_services.SaveFileDialog);
	}

	public override void put_DesignMode(int bDesignMode)
	{
		base.put_DesignMode(bDesignMode);
		if (FBConnector == null)
		{
			return;
		}

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
		if (_services == null && _serviceProvider == null)
		{
			return;
		}

		SafeDisposal.RunAll(UnsubscribeGlobalServices, DisposeRuntimeComponents);

		ResetRuntimeFields();
	}

	private void UnsubscribeGlobalServices()
	{
		if (_services != null)
		{
			_services.StateProvider.PermissionsChanged -= OnPermissionsChanged;
		}

		if (_permissionsDebounceTimer != null)
		{
			SafeDisposal.RunAll(
				() => _permissionsDebounceTimer.Stop(),
				() => _permissionsDebounceTimer.Tick -= PermissionsDebounceTimer_Tick,
				() => _permissionsDebounceTimer.Dispose());

			_permissionsDebounceTimer = null;
		}

		SafeDisposal.RunAll(() => _services?.StatusService.Detach());
	}

	private void DisposeRuntimeComponents()
	{
		_presenter?.Dispose();
		(_tableView as IDisposable)?.Dispose();
		_behaviorManager?.Dispose();
		_behaviorManager = null;

		_inputManager?.Dispose();
		_inputManager = null;
	}

	private void ResetRuntimeFields()
	{
		_presenter = null;
		_tableView = null;
		_serviceProvider = null;
		_services = null;
		_colorSchemeProvider = null;
		_runtimeInitialized = false;
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (!disposing)
			{
				return;
			}

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
		SafeDisposal.TryDispose(font);
	}
}
