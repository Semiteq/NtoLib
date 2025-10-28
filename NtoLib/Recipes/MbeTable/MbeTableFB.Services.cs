using System;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication.Services;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleConfig;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
{
    private void InitializeServices(ConfigurationState state)
    {
        if (_serviceProvider != null)
        {
            return;
        }

        try
        {
            _serviceProvider = MbeTableServiceConfigurator.ConfigureServices(this, state);

            _timerService = _serviceProvider.GetRequiredService<TimerService>();
            _runtimeState = _serviceProvider.GetRequiredService<IRecipeRuntimeState>();
            _actionTargetProvider = _serviceProvider.GetRequiredService<IActionTargetProvider>();

            var stateProvider = _serviceProvider.GetRequiredService<IStateProvider>();

            _timerService.TimesUpdated += OnTimesUpdated;
            _runtimeState.RecipeActiveChanged += recipeActive => stateProvider.SetPlcFlags(
                stateProvider.GetSnapshot().EnaSendOk,
                recipeActive);
            _runtimeState.SendEnabledChanged += sendEnabled => stateProvider.SetPlcFlags(
                sendEnabled,
                stateProvider.GetSnapshot().RecipeActive);
        }
        catch (Exception ex)
        {
            var fullMessage =
                $"Service initialization failed:\n\n{ex.GetType().Name}: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}";

            if (ex.InnerException != null)
            {
                fullMessage +=
                    $"\n\nInner Exception:\n{ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
            }

            MessageBox.Show(fullMessage, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void CleanupServices()
    {
        if (_serviceProvider == null)
        {
            return;
        }

        if (_timerService != null)
        {
            _timerService.TimesUpdated -= OnTimesUpdated;
        }

        if (_runtimeState != null)
        {
            var stateProvider = _serviceProvider.GetRequiredService<IStateProvider>();
            _runtimeState.RecipeActiveChanged -= recipeActive =>
            {
                if (stateProvider != null)
                    stateProvider.SetPlcFlags(
                        stateProvider.GetSnapshot().EnaSendOk,
                        recipeActive);
            };
            _runtimeState.SendEnabledChanged -= sendEnabled => stateProvider.SetPlcFlags(
                sendEnabled,
                stateProvider.GetSnapshot().RecipeActive);
        }

        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }

        _actionTargetProvider = null;
        _timerService = null;
        _runtimeState = null;
        _serviceProvider = null;
    }
}