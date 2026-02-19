using FluentResults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Csv;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Modbus;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policy;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policy.Registry;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleCore.State;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;
using NtoLib.Recipes.MbeTable.ServiceClipboard;
using NtoLib.Recipes.MbeTable.ServiceClipboard.Serialization;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Assembly;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Parsing;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Transform;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Common;

using Tests.MbeTable.Core.Helpers;

namespace Tests.MbeTable.Application.Clipboard.Helpers;

public static class ApplicationTestServiceProviderFactory
{
	public static ServiceProvider Create(AppConfiguration config,
		IReadOnlyDictionary<short, CompiledFormula> compiledFormulas)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));
		if (compiledFormulas == null)
			throw new ArgumentNullException(nameof(compiledFormulas));

		var services = new ServiceCollection();

		services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
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

		services.AddSingleton<AssemblyValidator>();
		services.AddSingleton<TargetAvailabilityValidator>();

		services.AddSingleton<FakeClipboardRawAccess>();
		services.AddSingleton<IClipboardRawAccess>(sp => sp.GetRequiredService<FakeClipboardRawAccess>());
		services.AddSingleton<ClipboardSerializationService>();
		services.AddSingleton<ClipboardService>();

		services.AddSingleton<ClipboardSchemaDescriptor>(sp =>
		{
			var columns = sp.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
			return new ClipboardSchemaDescriptor(columns);
		});
		services.AddSingleton<ClipboardSchemaValidator>();
		services.AddSingleton<ClipboardParser>();
		services.AddSingleton<ClipboardStepsTransformer>();
		services.AddSingleton<ClipboardAssemblyService>();

		services.AddSingleton<ErrorPolicyRegistry>();
		services.AddSingleton<PolicyEngine>();
		services.AddSingleton<StateProvider>();
		services.AddSingleton<PolicyReasonsSinkAdapter>();
		services.AddSingleton<IStatusPresenter, FakeStatusPresenter>();

		services.AddSingleton<RecipeViewModel>();

		services.AddSingleton<ICsvService, FakeCsvService>();
		services.AddSingleton<IModbusTcpService, FakeModbusTcpService>();

		services.AddSingleton<OperationPipelineRunner>();
		services.AddSingleton<RecipeOperationService>();

		services.AddSingleton<ForLoopNestingProvider>();

		return services.BuildServiceProvider();
	}

	private sealed class FakeCsvService : ICsvService
	{
		public Task<Result<Recipe>> ReadCsvAsync(string filePath)
			=> Task.FromResult(Result.Ok(Recipe.Empty));

		public Task<Result> WriteCsvAsync(Recipe recipe, string filePath)
			=> Task.FromResult(Result.Ok());
	}

	private sealed class FakeModbusTcpService : IModbusTcpService
	{
		public Task<Result> SendRecipeAsync(Recipe recipe)
			=> Task.FromResult(Result.Ok());

		public Task<Result<Recipe>> ReceiveRecipeAsync()
			=> Task.FromResult(Result.Ok(Recipe.Empty));
	}
}
