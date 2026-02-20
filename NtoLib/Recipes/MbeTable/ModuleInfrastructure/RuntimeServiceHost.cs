using System;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure;

internal sealed class RuntimeServiceHost : IDisposable
{
	private readonly MbeTableFB _owner;
	private readonly RecipeFacade _recipeFacade;
	private readonly RecipeRuntimeStatePoller _runtimeState;
	private readonly StateProvider _stateProvider;
	private readonly TimerService _timerService;

	public RuntimeServiceHost(IServiceProvider serviceProvider)
	{
		if (serviceProvider == null)
		{
			throw new ArgumentNullException(nameof(serviceProvider));
		}

		_timerService = serviceProvider.GetRequiredService<TimerService>();
		_recipeFacade = serviceProvider.GetRequiredService<RecipeFacade>();
		_runtimeState = serviceProvider.GetRequiredService<RecipeRuntimeStatePoller>();
		_stateProvider = serviceProvider.GetRequiredService<StateProvider>();
		_owner = serviceProvider.GetRequiredService<MbeTableFB>();

		_timerService.TimesUpdated += OnTimesUpdated;
		_stateProvider.RecipeConsistencyChanged += OnRecipeConsistentChanged;
		_runtimeState.RecipeActiveChanged += OnRecipeActiveChanged;
		_runtimeState.SendEnabledChanged += OnSendEnabledChanged;

		var snap = _stateProvider.GetSnapshot();
		_owner.UpdateRecipeConsistentPin(snap.IsRecipeConsistent);
	}

	public void Dispose()
	{
		_timerService.TimesUpdated -= OnTimesUpdated;
		_stateProvider.RecipeConsistencyChanged -= OnRecipeConsistentChanged;
		_runtimeState.RecipeActiveChanged -= OnRecipeActiveChanged;
		_runtimeState.SendEnabledChanged -= OnSendEnabledChanged;
	}

	public void Poll()
	{
		_runtimeState.Poll();
		var analysis = _recipeFacade.CurrentSnapshot;
		_timerService.UpdateRuntime(_runtimeState.Current, analysis);
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
}
