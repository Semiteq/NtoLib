using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.AddStep;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.CopySteps;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.CutSteps;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.DeleteSteps;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.EditCell;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Load;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.PasteSteps;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Recive;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Remove;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Save;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Send;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policy;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policy.Registry;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;
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
using NtoLib.Test.MbeTable.Core.Helpers;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

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

		services.AddSingleton<AssemblyValidator>();
		services.AddSingleton<TargetAvailabilityValidator>();

		services.AddSingleton<FakeClipboardRawAccess>();
		services.AddSingleton<IClipboardRawAccess>(sp => sp.GetRequiredService<FakeClipboardRawAccess>());
		services.AddSingleton<IClipboardSerializationService, ClipboardSerializationService>();
		services.AddSingleton<IClipboardService, ClipboardService>();

		services.AddSingleton<IClipboardSchemaDescriptor>(sp =>
		{
			var columns = sp.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
			return new ClipboardSchemaDescriptor(columns);
		});
		services.AddSingleton<IClipboardSchemaValidator, ClipboardSchemaValidator>();
		services.AddSingleton<IClipboardParser, ClipboardParser>();
		services.AddSingleton<IClipboardStepsTransformer, ClipboardStepsTransformer>();
		services.AddSingleton<IClipboardAssemblyService, ClipboardAssemblyService>();

		services.AddSingleton<ErrorPolicyRegistry>();
		services.AddSingleton<PolicyEngine>();
		services.AddSingleton<IStateProvider, StateProvider>();
		services.AddSingleton<PolicyReasonsSinkAdapter>();
		services.AddSingleton<IStatusPresenter, FakeStatusPresenter>();
		services.AddSingleton<OperationPipeline>();

		services.AddSingleton<RecipeViewModel>();

		services.AddSingleton<EditCellOperationDefinition>();
		services.AddSingleton<AddStepOperationDefinition>();
		services.AddSingleton<RemoveStepOperationDefinition>();
		services.AddSingleton<CopyRowsOperationDefinition>();
		services.AddSingleton<CutRowsOperationDefinition>();
		services.AddSingleton<PasteRowsOperationDefinition>();
		services.AddSingleton<DeleteRowsOperationDefinition>();

		services.AddSingleton<IRecipeOperationHandler<EditCellArgs>, EditCellOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<AddStepArgs>, AddStepOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<RemoveStepArgs>, RemoveStepOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<CopyRowsArgs>, CopyRowsOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<CutRowsArgs>, CutRowsOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<PasteRowsArgs>, PasteRowsOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<DeleteRowsArgs>, DeleteRowsOperationHandler>();

		services.AddSingleton<IRecipeOperationHandler<LoadRecipeArgs>, FakeLoadRecipeHandler>();
		services.AddSingleton<IRecipeOperationHandler<SaveRecipeArgs>, FakeSaveRecipeHandler>();
		services.AddSingleton<IRecipeOperationHandler<SendRecipeArgs>, FakeSendRecipeHandler>();
		services.AddSingleton<IRecipeOperationHandler<ReceiveRecipeArgs>, FakeReceiveRecipeHandler>();

		services.AddSingleton<IRecipeApplicationService, RecipeApplicationService>();

		services.AddSingleton<IForLoopNestingProvider, ForLoopNestingProvider>();

		return services.BuildServiceProvider();
	}

	private sealed class FakeLoadRecipeHandler : IRecipeOperationHandler<LoadRecipeArgs>
	{
		public Task<Result> ExecuteAsync(LoadRecipeArgs args) => Task.FromResult(Result.Ok());
	}

	private sealed class FakeSaveRecipeHandler : IRecipeOperationHandler<SaveRecipeArgs>
	{
		public Task<Result> ExecuteAsync(SaveRecipeArgs args) => Task.FromResult(Result.Ok());
	}

	private sealed class FakeSendRecipeHandler : IRecipeOperationHandler<SendRecipeArgs>
	{
		public Task<Result> ExecuteAsync(SendRecipeArgs args) => Task.FromResult(Result.Ok());
	}

	private sealed class FakeReceiveRecipeHandler : IRecipeOperationHandler<ReceiveRecipeArgs>
	{
		public Task<Result> ExecuteAsync(ReceiveRecipeArgs args) => Task.FromResult(Result.Ok());
	}
}
