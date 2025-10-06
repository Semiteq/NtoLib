using System;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.Application.Services;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Services;
using NtoLib.Recipes.MbeTable.Infrastructure;
using NtoLib.Recipes.MbeTable.Infrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

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

            // Activate bridge so it subscribes to runtime state events
            _ = _serviceProvider.GetRequiredService<PlcUiStateBridge>();

            _actionTargetProvider.RefreshTargets();
            _timerService.TimesUpdated += OnTimesUpdated;
        }
        catch (Exception ex)
        {
            var fullMessage = $"Service initialization failed:\n\n{ex.GetType().Name}: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}";
        
            if (ex.InnerException != null)
            {
                fullMessage += $"\n\nInner Exception:\n{ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
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