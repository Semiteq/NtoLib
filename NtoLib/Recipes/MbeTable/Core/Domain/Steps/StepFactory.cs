using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps.Defenitions;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps
{
    public class StepFactory
    {
        private readonly IReadOnlyDictionary<int, IStepDefaultsProvider> _defaultsProviders;
        private readonly PropertyDefinitionRegistry _registry;
        private TableSchema _tableSchema;

        public StepFactory(ActionManager actionManager, PropertyDefinitionRegistry registry, TableSchema tableSchema)
        {
            _registry = registry;
            _tableSchema = tableSchema;
            _defaultsProviders = new Dictionary<int, IStepDefaultsProvider>
            {
                { actionManager.Power.Id, new PowerDefaults(registry) },
                { actionManager.PowerSmooth.Id, new PowerSmoothDefaults(registry) },
                { actionManager.PowerWait.Id, new PowerWaitDefaults(registry) },
                { actionManager.Temperature.Id, new TemperatureDefaults(registry) },
                { actionManager.TemperatureSmooth.Id, new TemperatureSmoothDefaults(registry) },
                { actionManager.TemperatureWait.Id, new TemperatureWaitDefaults(registry) },
                { actionManager.Close.Id, new CloseDefaults(registry) },
                { actionManager.CloseAll.Id, new CloseAllDefaults(registry) },
                { actionManager.Open.Id, new OpenDefaults(registry) },
                { actionManager.OpenTime.Id, new OpenTimeDefaults(registry) },
                { actionManager.NRun.Id, new NitrogenRunSourceDefaults(registry) },
                { actionManager.NClose.Id, new NitrogenSourceCloseDefaults(registry) },
                { actionManager.NVent.Id, new NitrogenSourceVentDefaults(registry) },
                { actionManager.Pause.Id, new PauseDefaults(registry) },
                { actionManager.ForLoop.Id, new ForLoopDefaults(registry) },
                { actionManager.EndForLoop.Id, new EndForLoopDefaults(registry) },
                { actionManager.Wait.Id, new WaitDefaults(registry) }
            };
        }

        public StepBuilder For(int actionId)
        {
            if (!_defaultsProviders.TryGetValue(actionId, out var provider))
            {
                throw new KeyNotFoundException($"Defaults provider not found for action with id {actionId}");
            }

            return new StepBuilder(actionId, provider, _registry, _tableSchema);
        }
    }
}