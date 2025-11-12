using System;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure;

internal sealed class RuntimeServiceHost : IDisposable
{
    private readonly TimerService _timerService;
    private readonly IRecipeRuntimeState _runtimeState;
    private readonly IStateProvider _stateProvider;
    private readonly MbeTableFB _owner;

    public RuntimeServiceHost(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        _timerService = serviceProvider.GetRequiredService<TimerService>();
        _runtimeState = serviceProvider.GetRequiredService<IRecipeRuntimeState>();
        _stateProvider = serviceProvider.GetRequiredService<IStateProvider>();
        _owner = serviceProvider.GetRequiredService<MbeTableFB>();

        _timerService.TimesUpdated += OnTimesUpdated;
        _stateProvider.RecipeConsistencyChanged += OnRecipeConsistentChanged;
        _runtimeState.RecipeActiveChanged += OnRecipeActiveChanged;
        _runtimeState.SendEnabledChanged += OnSendEnabledChanged;
    }
    
    public void Poll()
    {
        _runtimeState.Poll();
        _timerService.UpdateFromSnapshot(_runtimeState.Current);
    }

    private void OnTimesUpdated(TimeSpan stepTimeLeft, TimeSpan totalTimeLeft)
    {
        _owner.UpdateTimerPins(stepTimeLeft, totalTimeLeft);
    }

    private void OnRecipeConsistentChanged(bool isRecipeConsistent)
    {
        _owner.UpdateRecipeConsistentPin(isRecipeConsistent);
    }
    
    private void OnRecipeActiveChanged(bool recipeActive)
    {
        var snapshot = _stateProvider.GetSnapshot();
        _stateProvider.SetPlcFlags(snapshot.EnaSendOk, recipeActive);
    }

    private void OnSendEnabledChanged(bool sendEnabled)
    {
        var snapshot = _stateProvider.GetSnapshot();
        _stateProvider.SetPlcFlags(sendEnabled, snapshot.RecipeActive);
    }

    public void Dispose()
    {
        _timerService.TimesUpdated -= OnTimesUpdated;
        _stateProvider.RecipeConsistencyChanged -= OnRecipeConsistentChanged;
        _runtimeState.RecipeActiveChanged -= OnRecipeActiveChanged;
        _runtimeState.SendEnabledChanged -= OnSendEnabledChanged;
    }
}