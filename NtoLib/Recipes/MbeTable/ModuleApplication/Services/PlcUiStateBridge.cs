using System;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

/// <summary>
/// Bridges PLC runtime flags to UI permissions by listening to runtime state events
/// and notifying UiPermissionService about EnaSend/RecipeActive changes.
/// </summary>
public sealed class PlcUiStateBridge : IDisposable
{
    private readonly IRecipeRuntimeState _runtime;
    private readonly IUiPermissionService _permissionService;
    private readonly ILogger<PlcUiStateBridge> _logger;
    private bool _disposed;

    public PlcUiStateBridge(IRecipeRuntimeState runtime, 
        IUiPermissionService permissionService, 
        ILogger<PlcUiStateBridge> logger)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _runtime.RecipeActiveChanged += OnFlagsChanged;
        _runtime.SendEnabledChanged += OnFlagsChanged;

        TryPushInitial();
    }

    private void OnFlagsChanged(bool _)
    {
        var snap = _runtime.Current;
        _permissionService.NotifyPlcStateChanged(snap.SendEnabled, snap.RecipeActive);
        _logger.LogDebug("PLC flags updated: EnaSendOk={EnaSendOk}, RecipeActive={RecipeActive}", snap.SendEnabled, snap.RecipeActive);
    }

    private void TryPushInitial()
    {
        var snap = _runtime.Current;
        _permissionService.NotifyPlcStateChanged(snap.SendEnabled, snap.RecipeActive);
        _logger.LogDebug("Initial PLC flags: EnaSendOk={EnaSendOk}, RecipeActive={RecipeActive}", snap.SendEnabled, snap.RecipeActive);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _runtime.RecipeActiveChanged -= OnFlagsChanged;
            _runtime.SendEnabledChanged -= OnFlagsChanged;
        }
        catch { }
    }
}