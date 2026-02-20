using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleCore.State;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;

namespace Tests.MbeTable.Core.Helpers;

public static class TestServiceProviderFactory
{
	public static ServiceProvider Create(AppConfiguration config,
		IReadOnlyDictionary<short, CompiledFormula> compiledFormulas, ILoggerFactory? loggerFactory = null)
	{
		if (config == null)
		{
			throw new ArgumentNullException(nameof(config));
		}

		if (compiledFormulas == null)
		{
			throw new ArgumentNullException(nameof(compiledFormulas));
		}

		var services = new ServiceCollection();

		services.AddSingleton<ILoggerFactory>(loggerFactory ?? NullLoggerFactory.Instance);
		services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

		services.AddSingleton(config);
		services.AddSingleton(config.Actions);
		services.AddSingleton(config.Columns);
		services.AddSingleton(config.PinGroupData);
		services.AddSingleton(config.PropertyDefinitions);

		services.AddSingleton(compiledFormulas);

		services.AddSingleton<ActionRepository>();
		services.AddSingleton<ComboboxDataProvider>();
		services.AddSingleton<PropertyDefinitionRegistry>();
		services.AddSingleton<PropertyStateProvider>();

		services.AddSingleton<StructureValidator>();
		services.AddSingleton<LoopParser>();
		services.AddSingleton<LoopSemanticEvaluator>();
		services.AddSingleton<TimingCalculator>();
		services.AddSingleton<RecipeAnalyzer>();
		services.AddSingleton<RecipeStateManager>();

		services.AddSingleton<FormulaEngine>();
		services.AddSingleton<StepVariableAdapter>();
		services.AddSingleton<FormulaApplicationCoordinator>();
		services.AddSingleton<RecipeMutator>();

		services.AddSingleton<IActionTargetProvider, FakeActionTargetProvider>();

		services.AddSingleton<RecipeFacade>();

		services.AddSingleton<TimerService>();

		return services.BuildServiceProvider();
	}
}
