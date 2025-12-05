using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NtoLib.MbeTable.ModuleApplication;
using NtoLib.MbeTable.ModuleApplication.Operations.Csv;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.AddStep;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.CopySteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.CutSteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.DeleteSteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.EditCell;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Load;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.PasteSteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Recive;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Remove;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Save;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Send;
using NtoLib.MbeTable.ModuleApplication.Operations.Modbus;
using NtoLib.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.MbeTable.ModuleApplication.Policy;
using NtoLib.MbeTable.ModuleApplication.Policy.Registry;
using NtoLib.MbeTable.ModuleApplication.State;
using NtoLib.MbeTable.ModuleApplication.Status;
using NtoLib.MbeTable.ModuleApplication.ViewModels;
using NtoLib.MbeTable.ModuleConfig.Domain;
using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModuleCore.Analyzer;
using NtoLib.MbeTable.ModuleCore.Facade;
using NtoLib.MbeTable.ModuleCore.Formulas;
using NtoLib.MbeTable.ModuleCore.Properties;
using NtoLib.MbeTable.ModuleCore.Runtime;
using NtoLib.MbeTable.ModuleCore.Services;
using NtoLib.MbeTable.ModuleCore.State;
using NtoLib.MbeTable.ModuleInfrastructure.ActionTarget;
using NtoLib.MbeTable.ModuleInfrastructure.PinDataManager;
using NtoLib.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.MbeTable.ModulePresentation.Columns;
using NtoLib.MbeTable.ModulePresentation.Columns.ComboBox;
using NtoLib.MbeTable.ModulePresentation.Columns.Text;
using NtoLib.MbeTable.ModulePresentation.Commands;
using NtoLib.MbeTable.ModulePresentation.DataAccess;
using NtoLib.MbeTable.ModulePresentation.Mapping;
using NtoLib.MbeTable.ModulePresentation.Rendering;
using NtoLib.MbeTable.ModulePresentation.State;
using NtoLib.MbeTable.ModulePresentation.StateProviders;
using NtoLib.MbeTable.ModulePresentation.Style;
using NtoLib.MbeTable.ServiceClipboard;
using NtoLib.MbeTable.ServiceClipboard.Serialization;
using NtoLib.MbeTable.ServiceCsv;
using NtoLib.MbeTable.ServiceCsv.Data;
using NtoLib.MbeTable.ServiceCsv.Integrity;
using NtoLib.MbeTable.ServiceCsv.IO;
using NtoLib.MbeTable.ServiceCsv.Metadata;
using NtoLib.MbeTable.ServiceCsv.Parsing;
using NtoLib.MbeTable.ServiceLogger;
using NtoLib.MbeTable.ServiceModbusTCP;
using NtoLib.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.MbeTable.ServiceModbusTCP.Protocol;
using NtoLib.MbeTable.ServiceModbusTCP.Transport;
using NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Assembly;
using NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Parsing;
using NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;
using NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Transform;
using NtoLib.MbeTable.ServiceRecipeAssembly.Common;
using NtoLib.MbeTable.ServiceRecipeAssembly.Csv;
using NtoLib.MbeTable.ServiceRecipeAssembly.Modbus;
using NtoLib.MbeTable.ServiceStatus;

using Serilog;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NtoLib.MbeTable.ModuleInfrastructure;

public static class MbeTableServiceConfigurator
{
	public static IServiceProvider ConfigureServices(MbeTableFB mbeTableFb,
		AppConfiguration configurationState,
		IReadOnlyDictionary<short, CompiledFormula> compiledFormulas)
	{
		var services = new ServiceCollection();

		var runtimeOptionsProvider = new FbRuntimeOptionsProvider(mbeTableFb);
		services.AddSingleton<IRuntimeOptionsProvider>(runtimeOptionsProvider);

		RegisterLogging(services);
		RegisterConfiguration(services, configurationState);
		RegisterSharedInstances(services, mbeTableFb, configurationState);
		RegisterRuntimeState(services, mbeTableFb);
		RegisterLoggerServices(services);
		RegisterStatusService(services);
		RegisterCompiledFormulas(services, compiledFormulas);
		RegisterCoreServices(services);
		RegisterAnalyzerPipeline(services);
		RegisterCsvServices(services);
		RegisterInfrastructureServices(services);
		RegisterModbusTcpServices(services);
		RegisterRecipeAssemblyServices(services);
		RegisterApplicationServices(services);
		RegisterPresentationServices(services);

		var serviceProvider = services.BuildServiceProvider();
		var loggingBootstrapper = serviceProvider.GetRequiredService<LoggingBootstrapper>();
		loggingBootstrapper.Initialize();

		return serviceProvider;
	}

	private static void RegisterLogging(IServiceCollection services)
	{
		services.AddSingleton<LoggingOptions>();
		services.AddSingleton<LoggingBootstrapper>();
		services.AddLogging(builder => builder.AddSerilog(dispose: false));
		services.AddSingleton<ILogger>(sp =>
		{
			var factory = sp.GetRequiredService<ILoggerFactory>();
			return factory.CreateLogger("NtoLib.Recipes.MbeTable");
		});
	}

	private static void RegisterConfiguration(IServiceCollection services, AppConfiguration configurationState)
	{
		services.AddSingleton(configurationState.Actions);
		services.AddSingleton(configurationState.Columns);
		services.AddSingleton(configurationState.PinGroupData);
		services.AddSingleton(configurationState.PropertyDefinitions);
	}

	private static void RegisterCompiledFormulas(
		IServiceCollection services,
		IReadOnlyDictionary<short, CompiledFormula> compiledFormulas)
	{
		services.AddSingleton(compiledFormulas);
	}

	private static void RegisterSharedInstances(
		IServiceCollection services,
		MbeTableFB mbeTableFb,
		AppConfiguration configurationState)
	{
		services.AddSingleton(mbeTableFb);
		services.AddSingleton(configurationState);
		services.AddSingleton<IPinAccessor>(_ => new FbPinAccessor(mbeTableFb));
	}

	private static void RegisterRuntimeState(IServiceCollection services, MbeTableFB fb)
	{
		services.AddSingleton<IRecipeRuntimeState>(sp =>
		{
			var accessor = sp.GetRequiredService<IPinAccessor>();
			return new RecipeRuntimeStatePoller(
				accessor,
				fb.Epsilon,
				MbeTableFB.IdRecipeActive,
				MbeTableFB.IdEnaSend,
				MbeTableFB.IdCurrentLine,
				MbeTableFB.IdForLoopCount1,
				MbeTableFB.IdForLoopCount2,
				MbeTableFB.IdForLoopCount3,
				MbeTableFB.IdStepCurrentTime);
		});
	}

	private static void RegisterLoggerServices(IServiceCollection services)
	{
		services.AddSingleton(_ => new StatusFormatter(280));
	}

	private static void RegisterStatusService(IServiceCollection services)
	{
		services.AddSingleton<IStatusService, StatusService>();
	}

	private static void RegisterCoreServices(IServiceCollection services)
	{
		services.AddSingleton<IActionRepository, ActionRepository>();
		services.AddSingleton<IComboboxDataProvider, ComboboxDataProvider>();
		services.AddSingleton<PropertyDefinitionRegistry>();
		services.AddSingleton<PropertyStateProvider>();
		services.AddSingleton<RecipeMutator>();
		services.AddSingleton<IFormulaEngine, FormulaEngine>();
		services.AddSingleton<IStepVariableAdapter, StepVariableAdapter>();
		services.AddSingleton<FormulaApplicationCoordinator>();
		services.AddSingleton<IRecipeFacade, RecipeFacade>();
		services.AddSingleton<ITimerService, TimerService>();
		services.AddSingleton<IForLoopNestingProvider, ForLoopNestingProvider>();
	}

	private static void RegisterAnalyzerPipeline(IServiceCollection services)
	{
		services.AddSingleton<IStructureValidator, StructureValidator>();
		services.AddSingleton<ILoopParser, LoopParser>();
		services.AddSingleton<ILoopSemanticEvaluator, LoopSemanticEvaluator>();
		services.AddSingleton<ITimingCalculator, TimingCalculator>();
		services.AddSingleton<IRecipeAnalyzer, RecipeAnalyzer>();
		services.AddSingleton<IRecipeStateManager, RecipeStateManager>();
	}

	private static void RegisterCsvServices(IServiceCollection services)
	{
		services.AddSingleton<ICsvHelperFactory, CsvHelperFactory>();
		services.AddSingleton<ICsvDataExtractor, CsvDataExtractor>();
		services.AddSingleton<ICsvDataFormatter, CsvDataFormatter>();
		services.AddSingleton<ICsvHeaderBinder, CsvHeaderBinder>();
		services.AddSingleton<IMetadataService, MetadataService>();
		services.AddSingleton<RecipeFileMetadataSerializer>();
		services.AddSingleton<IIntegrityService, IntegrityService>();
		services.AddSingleton<IRecipeReader, RecipeReader>();
		services.AddSingleton<IRecipeWriter, RecipeWriter>();
		services.AddSingleton<CsvAssemblyStrategy>();
		services.AddSingleton<IRecipeFileService, RecipeFileService>();
	}

	private static void RegisterInfrastructureServices(IServiceCollection services)
	{
		services.AddSingleton<IActionTargetProvider, ActionTargetProvider>();
	}

	private static void RegisterModbusTcpServices(IServiceCollection services)
	{
		services.AddSingleton<RecipeColumnLayout>(sp =>
		{
			var columns = sp.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
			return new RecipeColumnLayout(columns);
		});

		services.AddSingleton<IModbusChunkHandler, ModbusChunkHandler>();
		services.AddSingleton<PlcCapacityCalculator>();
		services.AddSingleton<RecipeComparator>();
		services.AddSingleton<PlcRecipeSerializer>();
		services.AddSingleton<MagicNumberValidator>();
		services.AddSingleton<ModbusConnectionManager>();
		services.AddSingleton<IModbusTransport, ModbusTransport>();
		services.AddSingleton<IPlcReader, PlcReader>();
		services.AddSingleton<IPlcWriter, PlcWriter>();
		services.AddSingleton<IDisconnectStrategy, KeepAliveStrategy>();
		services.AddSingleton<IRecipePlcService, RecipePlcService>();
	}

	private static void RegisterRecipeAssemblyServices(IServiceCollection services)
	{
		services.AddSingleton<ModbusAssemblyStrategy>();
		services.AddSingleton<AssemblyValidator>();
		services.AddSingleton<TargetAvailabilityValidator>();
		services.AddSingleton<IModbusRecipeAssemblyService, ModbusRecipeAssemblyService>();
		services.AddSingleton<ICsvRecipeAssemblyService, CsvRecipeAssemblyService>();

		services.AddSingleton<IClipboardSchemaDescriptor>(sp =>
		{
			var columns = sp.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
			return new ClipboardSchemaDescriptor(columns);
		});
		services.AddSingleton<IClipboardSchemaValidator, ClipboardSchemaValidator>();
		services.AddSingleton<IClipboardParser, ClipboardParser>();
		services.AddSingleton<IClipboardStepsTransformer, ClipboardStepsTransformer>();
		services.AddSingleton<IClipboardAssemblyService, ClipboardAssemblyService>();
	}

	private static void RegisterApplicationServices(IServiceCollection services)
	{
		services.AddSingleton<ErrorPolicyRegistry>();
		services.AddSingleton<PolicyEngine>();
		services.AddSingleton<IStateProvider, StateProvider>();
		services.AddSingleton<PolicyReasonsSinkAdapter>();
		services.AddSingleton<IStatusPresenter, StatusPresenter>();
		services.AddSingleton<OperationPipeline>();

		// ServiceClipboard services
		services.AddSingleton<IClipboardRawAccess, SystemClipboardRawAccess>();
		services.AddSingleton<IClipboardSerializationService, ClipboardSerializationService>();
		services.AddSingleton<IClipboardService, ClipboardService>();

		services.AddSingleton<ActionComboBox>();
		services.AddSingleton<TargetComboBox>();
		services.AddSingleton<TextBoxExtension>();
		services.AddSingleton<StepStartTime>();
		services.AddSingleton<RecipeViewModel>();
		services.AddSingleton<IModbusTcpService, ModbusTcpService>();
		services.AddSingleton<ICsvService, CsvService>();
		services.AddSingleton<IRecipeApplicationService, RecipeApplicationService>();

		// Existing operation definitions
		services.AddSingleton<EditCellOperationDefinition>();
		services.AddSingleton<AddStepOperationDefinition>();
		services.AddSingleton<RemoveStepOperationDefinition>();
		services.AddSingleton<LoadRecipeOperationDefinition>();
		services.AddSingleton<SaveRecipeOperationDefinition>();
		services.AddSingleton<SendRecipeOperationDefinition>();
		services.AddSingleton<ReceiveRecipeOperationDefinition>();

		// Clipboard operation definitions
		services.AddSingleton<CopyRowsOperationDefinition>();
		services.AddSingleton<CutRowsOperationDefinition>();
		services.AddSingleton<PasteRowsOperationDefinition>();
		services.AddSingleton<DeleteRowsOperationDefinition>();

		// Existing operation handlers
		services.AddSingleton<IRecipeOperationHandler<EditCellArgs>, EditCellOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<AddStepArgs>, AddStepOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<RemoveStepArgs>, RemoveStepOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<LoadRecipeArgs>, LoadRecipeOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<SaveRecipeArgs>, SaveRecipeOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<SendRecipeArgs>, SendRecipeOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<ReceiveRecipeArgs>, ReceiveRecipeOperationHandler>();

		// Clipboard operation handlers
		services.AddSingleton<IRecipeOperationHandler<CopyRowsArgs>, CopyRowsOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<CutRowsArgs>, CutRowsOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<PasteRowsArgs>, PasteRowsOperationHandler>();
		services.AddSingleton<IRecipeOperationHandler<DeleteRowsArgs>, DeleteRowsOperationHandler>();
	}

	private static void RegisterPresentationServices(IServiceCollection services)
	{
		services.AddSingleton<IBusyStateManager, BusyStateManager>();
		services.AddSingleton<ICellStateResolver, CellStateResolver>();
		services.AddSingleton<IRowExecutionStateProvider, ThreadSafeRowExecutionStateProvider>();
		services.AddSingleton<IColumnAlignmentResolver, ColumnAlignmentResolver>();
		services.AddSingleton<IAlignmentMapper, AlignmentMapper>();
		services.AddSingleton<IColorSchemeProvider, DesignTimeColorSchemeProvider>();
		services.AddSingleton<ColorScheme>();
		services.AddSingleton<ICellDataContext, CellDataContext>();
		services.AddSingleton<ActionItemsProvider>();
		services.AddSingleton<TargetItemsProvider>();
		services.AddSingleton<ICellRenderer, ComboBoxCellRenderer>();
		services.AddScoped<ITableRenderCoordinator, TableRenderCoordinator>();
		services.AddSingleton<FactoryColumnRegistry>();

		services.AddSingleton<LoadRecipeCommand>();
		services.AddSingleton<SaveRecipeCommand>();
		services.AddSingleton<SendRecipeCommand>();
		services.AddSingleton<ReceiveRecipeCommand>();
		services.AddSingleton<RemoveStepCommand>();
		services.AddSingleton<AddStepCommand>();

		services.AddSingleton<CopyRowsCommand>();
		services.AddSingleton<CutRowsCommand>();
		services.AddSingleton<PasteRowsCommand>();
		services.AddSingleton<DeleteRowsCommand>();
		services.AddSingleton<InsertRowCommand>();

		services.AddSingleton(_ => new OpenFileDialog
		{
			Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
			AddExtension = true,
			Multiselect = false,
			Title = @"Select recipe file"
		});

		services.AddSingleton(_ => new SaveFileDialog
		{
			Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
			AddExtension = true,
			Title = @"Save recipe file"
		});
	}
}
