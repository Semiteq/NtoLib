using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.MbeTable.ModuleConfig.Domain;
using NtoLib.MbeTable.ModuleCore.Analyzer;
using NtoLib.MbeTable.ModuleCore.Facade;
using NtoLib.MbeTable.ModuleCore.Formulas;
using NtoLib.MbeTable.ModuleCore.Properties;
using NtoLib.MbeTable.ModuleCore.Runtime;
using NtoLib.MbeTable.ModuleCore.Services;
using NtoLib.MbeTable.ModuleCore.State;
using NtoLib.MbeTable.ModuleInfrastructure.ActionTarget;

namespace NtoLib.Test.MbeTable.Core.Helpers;

public static class TestServiceProviderFactory
{
	public static ServiceProvider Create(AppConfiguration config,
		IReadOnlyDictionary<short, CompiledFormula> compiledFormulas, ILoggerFactory? loggerFactory = null)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		if (compiledFormulas == null)
			throw new ArgumentNullException(nameof(compiledFormulas));

		var services = new ServiceCollection();

		services.AddSingleton<ILoggerFactory>(loggerFactory ?? NullLoggerFactory.Instance);
		services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

		services.AddSingleton(config);
		services.AddSingleton(config.Actions);
		services.AddSingleton(config.Columns);
		services.AddSingleton(config.PinGroupData);
		services.AddSingleton(config.PropertyDefinitions);

		services.AddSingleton(compiledFormulas);

		services.AddSingleton<IActionRepository, ActionRepository>();
		services.AddSingleton<IComboboxDataProvider, ComboboxDataProvider>();
		services.AddSingleton<PropertyDefinitionRegistry>();
		services.AddSingleton<PropertyStateProvider>();

		services.AddSingleton<IStructureValidator, StructureValidator>();
		services.AddSingleton<ILoopParser, LoopParser>();
		services.AddSingleton<ILoopSemanticEvaluator, LoopSemanticEvaluator>();
		services.AddSingleton<ITimingCalculator, TimingCalculator>();
		services.AddSingleton<IRecipeAnalyzer, RecipeAnalyzer>();
		services.AddSingleton<IRecipeStateManager, RecipeStateManager>();

		services.AddSingleton<IFormulaEngine, FormulaEngine>();
		services.AddSingleton<IStepVariableAdapter, StepVariableAdapter>();
		services.AddSingleton<FormulaApplicationCoordinator>();
		services.AddSingleton<RecipeMutator>();

		services.AddSingleton<IActionTargetProvider, FakeActionTargetProvider>();

		services.AddSingleton<IRecipeFacade, RecipeFacade>();

		services.AddSingleton<ITimerService, TimerService>();

		return services.BuildServiceProvider();
	}
}
