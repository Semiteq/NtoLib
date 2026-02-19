using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication;
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
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleCore.State;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ModulePresentation;
using NtoLib.Recipes.MbeTable.ModulePresentation.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Columns.ComboBox;
using NtoLib.Recipes.MbeTable.ModulePresentation.Columns.Text;
using NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;
using NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;
using NtoLib.Recipes.MbeTable.ServiceClipboard;
using NtoLib.Recipes.MbeTable.ServiceClipboard.Serialization;
using NtoLib.Recipes.MbeTable.ServiceCsv;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;
using NtoLib.Recipes.MbeTable.ServiceCsv.IO;
using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;
using NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;
using NtoLib.Recipes.MbeTable.ServiceLogger;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Assembly;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Parsing;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Transform;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Common;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Csv;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Modbus;
using NtoLib.Recipes.MbeTable.ServiceStatus;

using Serilog;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure;

public static class MbeTableServiceConfigurator
{
	public static IServiceProvider ConfigureServices(MbeTableFB mbeTableFb,
		AppConfiguration configurationState,
		IReadOnlyDictionary<short, CompiledFormula> compiledFormulas)
	{
		var services = new ServiceCollection();

		var runtimeOptionsProvider = new FbRuntimeOptionsProvider(mbeTableFb);
		services.AddSingleton(runtimeOptionsProvider);

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
		services.AddSingleton<FbPinAccessor>(_ => new FbPinAccessor(mbeTableFb));
	}

	private static void RegisterRuntimeState(IServiceCollection services, MbeTableFB fb)
	{
		services.AddSingleton<RecipeRuntimeStatePoller>(sp =>
		{
			var accessor = sp.GetRequiredService<FbPinAccessor>();
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
		services.AddSingleton<StatusService>();
	}

	private static void RegisterCoreServices(IServiceCollection services)
	{
		services.AddSingleton<ActionRepository>();
		services.AddSingleton<ComboboxDataProvider>();
		services.AddSingleton<PropertyDefinitionRegistry>();
		services.AddSingleton<PropertyStateProvider>();
		services.AddSingleton<RecipeMutator>();
		services.AddSingleton<FormulaEngine>();
		services.AddSingleton<StepVariableAdapter>();
		services.AddSingleton<FormulaApplicationCoordinator>();
		services.AddSingleton<RecipeFacade>();
		services.AddSingleton<TimerService>();
		services.AddSingleton<ForLoopNestingProvider>();
	}

	private static void RegisterAnalyzerPipeline(IServiceCollection services)
	{
		services.AddSingleton<StructureValidator>();
		services.AddSingleton<LoopParser>();
		services.AddSingleton<LoopSemanticEvaluator>();
		services.AddSingleton<TimingCalculator>();
		services.AddSingleton<RecipeAnalyzer>();
		services.AddSingleton<RecipeStateManager>();
	}

	private static void RegisterCsvServices(IServiceCollection services)
	{
		services.AddSingleton<CsvHelperFactory>();
		services.AddSingleton<CsvDataExtractor>();
		services.AddSingleton<CsvDataFormatter>();
		services.AddSingleton<CsvHeaderBinder>();
		services.AddSingleton<MetadataService>();
		services.AddSingleton<RecipeFileMetadataSerializer>();
		services.AddSingleton<IntegrityService>();
		services.AddSingleton<RecipeReader>();
		services.AddSingleton<RecipeWriter>();
		services.AddSingleton<CsvAssemblyStrategy>();
		services.AddSingleton<RecipeFileService>();
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

		services.AddSingleton<ModbusChunkHandler>();
		services.AddSingleton<PlcCapacityCalculator>();
		services.AddSingleton<RecipeComparator>();
		services.AddSingleton<PlcRecipeSerializer>();
		services.AddSingleton<MagicNumberValidator>();
		services.AddSingleton<ModbusConnectionManager>();
		services.AddSingleton<ModbusTransport>();
		services.AddSingleton<PlcReader>();
		services.AddSingleton<PlcWriter>();
		services.AddSingleton<IDisconnectStrategy, KeepAliveStrategy>();
		services.AddSingleton<RecipePlcService>();
	}

	private static void RegisterRecipeAssemblyServices(IServiceCollection services)
	{
		services.AddSingleton<ModbusAssemblyStrategy>();
		services.AddSingleton<AssemblyValidator>();
		services.AddSingleton<TargetAvailabilityValidator>();
		services.AddSingleton<ModbusRecipeAssemblyService>();
		services.AddSingleton<CsvRecipeAssemblyService>();

		services.AddSingleton<ClipboardSchemaDescriptor>(sp =>
		{
			var columns = sp.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
			return new ClipboardSchemaDescriptor(columns);
		});
		services.AddSingleton<ClipboardSchemaValidator>();
		services.AddSingleton<ClipboardParser>();
		services.AddSingleton<ClipboardStepsTransformer>();
		services.AddSingleton<ClipboardAssemblyService>();
	}

	private static void RegisterApplicationServices(IServiceCollection services)
	{
		services.AddSingleton<ErrorPolicyRegistry>();
		services.AddSingleton<PolicyEngine>();
		services.AddSingleton<StateProvider>();
		services.AddSingleton<PolicyReasonsSinkAdapter>();
		services.AddSingleton<IStatusPresenter, StatusPresenter>();

		// ServiceClipboard services
		services.AddSingleton<IClipboardRawAccess, SystemClipboardRawAccess>();
		services.AddSingleton<ClipboardSerializationService>();
		services.AddSingleton<ClipboardService>();

		services.AddSingleton<ActionComboBox>();
		services.AddSingleton<TargetComboBox>();
		services.AddSingleton<TextBoxExtension>();
		services.AddSingleton<StepStartTime>();
		services.AddSingleton<RecipeViewModel>();
		services.AddSingleton<IModbusTcpService, ModbusTcpService>();
		services.AddSingleton<ICsvService, CsvService>();
		services.AddSingleton<OperationPipelineRunner>();
		services.AddSingleton<RecipeOperationService>();
	}

	private static void RegisterPresentationServices(IServiceCollection services)
	{
		services.AddSingleton<BusyStateManager>();
		services.AddSingleton<CellStateResolver>();
		services.AddSingleton<ThreadSafeRowExecutionStateProvider>();
		services.AddSingleton<DesignTimeColorSchemeProvider>();
		services.AddSingleton<ColorScheme>();
		services.AddSingleton<CellDataContext>();
		services.AddSingleton<ActionItemsProvider>();
		services.AddSingleton<TargetItemsProvider>();
		services.AddSingleton<ComboBoxCellRenderer>();
		services.AddScoped<TableRenderCoordinator>();
		services.AddSingleton<FactoryColumnRegistry>();
		services.AddSingleton<TableControlServices>();

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
