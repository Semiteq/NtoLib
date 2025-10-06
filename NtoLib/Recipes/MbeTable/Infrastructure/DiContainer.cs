using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Application;
using NtoLib.Recipes.MbeTable.Application.Operations;
using NtoLib.Recipes.MbeTable.Application.Services;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core;
using NtoLib.Recipes.MbeTable.Core.Attributes;
using NtoLib.Recipes.MbeTable.Core.Properties;
using NtoLib.Recipes.MbeTable.Core.Services;
using NtoLib.Recipes.MbeTable.Csv;
using NtoLib.Recipes.MbeTable.Csv.Data;
using NtoLib.Recipes.MbeTable.Csv.Integrity;
using NtoLib.Recipes.MbeTable.Csv.IO;
using NtoLib.Recipes.MbeTable.Csv.Metadata;
using NtoLib.Recipes.MbeTable.Csv.Parsing;
using NtoLib.Recipes.MbeTable.Infrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Infrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.Journaling.Status;
using NtoLib.Recipes.MbeTable.Logging;
using NtoLib.Recipes.MbeTable.ModbusTCP;
using NtoLib.Recipes.MbeTable.ModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ModbusTCP.Protocol;
using NtoLib.Recipes.MbeTable.ModbusTCP.Transport;
using NtoLib.Recipes.MbeTable.Presentation.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Commands;
using NtoLib.Recipes.MbeTable.Presentation.DataAccess;
using NtoLib.Recipes.MbeTable.Presentation.Rendering;
using NtoLib.Recipes.MbeTable.Presentation.State;
using NtoLib.Recipes.MbeTable.Presentation.StateProviders;
using NtoLib.Recipes.MbeTable.Presentation.Style;
using NtoLib.Recipes.MbeTable.RecipeAssemblyService;
using NtoLib.Recipes.MbeTable.RecipeAssemblyService.Strategies;
using NtoLib.Recipes.MbeTable.RecipeAssemblyService.Validation;

namespace NtoLib.Recipes.MbeTable.Infrastructure;

public static class MbeTableServiceConfigurator
{
    public static IServiceProvider ConfigureServices(MbeTableFB mbeTableFb, ConfigurationState configurationState)
    {
        var services = new ServiceCollection();

        var runtimeOptionsProvider = new FbRuntimeOptionsProvider(mbeTableFb);
        services.AddSingleton<IRuntimeOptionsProvider>(runtimeOptionsProvider);

        RegisterLogging(services, runtimeOptionsProvider);

        RegisterConfiguration(services, configurationState);
        RegisterSharedInstances(services, mbeTableFb, configurationState);
        RegisterRuntimeState(services, mbeTableFb);

        RegisterJournalingServices(services);
        RegisterCoreServices(services);
        RegisterCsvServices(services);
        RegisterInfrastructureServices(services);
        RegisterModbusTcpServices(services);
        RegisterRecipeAssemblyServices(services);
        RegisterApplicationServices(services);
        RegisterPresentationServices(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterLogging(IServiceCollection services, IRuntimeOptionsProvider runtimeOptionsProvider)
    {
        var opts = runtimeOptionsProvider.GetCurrent();
        var loggingOptions = new LoggingOptions(opts.LogToFile, opts.LogFilePath);

        services.AddSingleton(sp => new LoggingBootstrapper(loggingOptions));
        services.AddSingleton<ILoggerFactory>(sp => sp.GetRequiredService<LoggingBootstrapper>().Factory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<ILogger>(sp =>
        {
            var factory = sp.GetRequiredService<ILoggerFactory>();
            return factory.CreateLogger("NtoLib.Recipes.MbeTable");
        });
    }

    private static void RegisterConfiguration(IServiceCollection services, ConfigurationState configurationState)
    {
        services.AddSingleton(configurationState.AppConfiguration.Actions);
        services.AddSingleton(configurationState.AppConfiguration.Columns);
        services.AddSingleton(configurationState.AppConfiguration.PinGroupData);
        services.AddSingleton(configurationState.AppConfiguration.PropertyDefinitions);
    }

    private static void RegisterSharedInstances(
        IServiceCollection services,
        MbeTableFB mbeTableFb,
        ConfigurationState configurationState)
    {
        services.AddSingleton(mbeTableFb);
        services.AddSingleton(configurationState);
        services.AddSingleton(configurationState.AppConfiguration);
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

    private static void RegisterJournalingServices(IServiceCollection services)
    {
        services.AddSingleton<IStatusManager, StatusManager>();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton<IActionRepository, ActionRepository>();
        services.AddSingleton<IComboboxDataProvider, ComboboxDataProvider>();
        services.AddSingleton<PropertyDefinitionRegistry>();
        services.AddSingleton<PropertyStateProvider>();
        services.AddSingleton<TimerService>();

        services.AddSingleton<RecipeStructureValidator>();
        services.AddSingleton<RecipeLoopValidator>();
        services.AddSingleton<RecipeTimeCalculator>();
        services.AddSingleton<IRecipeAttributesService, RecipeAttributesService>();

        services.AddSingleton<RecipeMutator>();
        services.AddSingleton<IRecipeService>(sp =>
        {
            var mutator = sp.GetRequiredService<RecipeMutator>();
            var attributesService = sp.GetRequiredService<IRecipeAttributesService>();
            return new RecipeService(mutator, attributesService);
        });
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
        services.AddSingleton<AssemblyValidator>();

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
        services.AddSingleton<PlcCapacityCalculator>();
        services.AddSingleton<RecipeComparator>();
        services.AddSingleton<PlcRecipeSerializer>();
        services.AddSingleton<IPlcProtocol, PlcProtocol>();
        services.AddSingleton<IModbusTransport, ModbusTransport>();
        services.AddSingleton<IRecipePlcService, RecipePlcService>();
    }

    private static void RegisterRecipeAssemblyServices(IServiceCollection services)
    {
        services.AddSingleton<ModbusAssemblyStrategy>();

        services.AddSingleton<AssemblyValidator>();
        services.AddSingleton<TargetAvailabilityValidator>();

        services.AddSingleton<IRecipeAssemblyService, RecipeAssemblyService.RecipeAssemblyService>();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<UiStateManager>();
        services.AddSingleton<IUiStateService>(sp =>
        {
            var manager = sp.GetRequiredService<UiStateManager>();
            var logger = sp.GetRequiredService<ILogger>();
            return new UiStateService(manager, logger);
        });

        services.AddSingleton<RecipeViewModel>(sp =>
        {
            var recipeService = sp.GetRequiredService<IRecipeService>();
            var comboboxData = sp.GetRequiredService<IComboboxDataProvider>();
            var propertyState = sp.GetRequiredService<PropertyStateProvider>();
            var columns = sp.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
            var logger = sp.GetRequiredService<ILogger>();
            return new RecipeViewModel(recipeService, comboboxData, propertyState, columns, logger);
        });

        services.AddSingleton<IModbusTcpService>(sp =>
        {
            var plcService = sp.GetRequiredService<IRecipePlcService>();
            var assemblyService = sp.GetRequiredService<IRecipeAssemblyService>();
            var comparator = sp.GetRequiredService<RecipeComparator>();
            return new ModbusTcpService(plcService, assemblyService, comparator);
        });

        services.AddSingleton<ICsvService>(sp =>
        {
            var fileService = sp.GetRequiredService<IRecipeFileService>();
            var assemblyService = sp.GetRequiredService<IRecipeAssemblyService>();
            var validator = sp.GetRequiredService<AssemblyValidator>();
            var logger = sp.GetRequiredService<ILogger>();
            return new CsvService(fileService, assemblyService, validator, logger);
        });

        services.AddSingleton<IRecipeApplicationService>(sp =>
        {
            var recipeService = sp.GetRequiredService<IRecipeService>();
            var modbusTcpService = sp.GetRequiredService<IModbusTcpService>();
            var csvOperations = sp.GetRequiredService<ICsvService>();
            var uiStateService = sp.GetRequiredService<IUiStateService>();
            var viewModel = sp.GetRequiredService<RecipeViewModel>();
            var logger = sp.GetRequiredService<ILogger>();

            return new RecipeApplicationService(
                recipeService,
                modbusTcpService,
                csvOperations,
                uiStateService,
                viewModel,
                logger);
        });
        
        services.AddSingleton<PlcUiStateBridge>();
    }

    private static void RegisterPresentationServices(IServiceCollection services)
    {
        services.AddSingleton<IBusyStateManager, BusyStateManager>();
        services.AddSingleton<ICellStateResolver, CellStateResolver>();
        services.AddSingleton<IRowExecutionStateProvider, ThreadSafeRowExecutionStateProvider>();

        services.AddSingleton<IColorSchemeProvider, DesignTimeColorSchemeProvider>();
        services.AddSingleton<ColorScheme>();

        services.AddSingleton<ICellDataContext, CellDataContext>();
        services.AddSingleton<ActionItemsProvider>();
        services.AddSingleton<TargetItemsProvider>();

        services.AddSingleton<ICellRenderer, ComboBoxCellRenderer>();

        services.AddScoped<ITableRenderCoordinator, TableRenderCoordinator>();

        services.AddTransient<ActionComboBoxColumnFactory>();
        services.AddTransient<ActionTargetComboBoxColumnFactory>();
        services.AddTransient<TextBoxColumnFactory>();
        services.AddTransient<PropertyColumnFactory>();
        services.AddTransient<StepStartTimeColumnFactory>();

        services.AddSingleton<IColumnFactoryRegistry>(sp =>
        {
            var registry = new ColumnFactoryRegistry(sp);
            registry.RegisterDefaultMappings();
            return registry;
        });

        services.AddSingleton<LoadRecipeCommand>();
        services.AddSingleton<SaveRecipeCommand>();
        services.AddSingleton<SendRecipeCommand>();
        services.AddSingleton<ReceiveRecipeCommand>();
        services.AddSingleton<RemoveStepCommand>();
        services.AddSingleton<AddStepCommand>();

        services.AddSingleton(_ => new OpenFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            Multiselect = false,
            Title = @"Select recipe file",
            RestoreDirectory = true
        });

        services.AddSingleton(_ => new SaveFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            Title = @"Save recipe file",
            RestoreDirectory = true
        });
    }
}