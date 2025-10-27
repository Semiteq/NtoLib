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
    private bool _disposed;

    public PlcUiStateBridge(IRecipeRuntimeState runtime, 
        IUiPermissionService permissionService)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

        _runtime.RecipeActiveChanged += OnFlagsChanged;
        _runtime.SendEnabledChanged += OnFlagsChanged;

        TryPushInitial();
    }

    private void OnFlagsChanged(bool _)
    {
        var snap = _runtime.Current;
        _permissionService.NotifyPlcStateChanged(snap.SendEnabled, snap.RecipeActive);
    }

    private void TryPushInitial()
    {
        var snap = _runtime.Current;
        _permissionService.NotifyPlcStateChanged(snap.SendEnabled, snap.RecipeActive);
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