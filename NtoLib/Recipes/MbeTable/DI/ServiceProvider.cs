#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;
using NtoLib.Recipes.MbeTable.Config.Yaml.Validators;
using NtoLib.Recipes.MbeTable.Core.Application.Services;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Calculations;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Transport;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;
using NtoLib.Recipes.MbeTable.StateMachine;
using NtoLib.Recipes.MbeTable.StateMachine.App;
using NtoLib.Recipes.MbeTable.StateMachine.Contracts;
using NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

namespace NtoLib.Recipes.MbeTable.DI;

/// <summary>
/// Composes services and application configuration for MBE Table module.
/// </summary>
public static class MbeTableServiceConfigurator
{
    private const string ConfigFolderName = "NtoLibTableConfig";

    private static readonly string[] DefaultConfigFiles =
    {
        "ColumnDefs.yaml",
        "ActionsDefs.yaml",
        "PinGroupDefs.yaml",
        "PropertyDefs.yaml"
    };

    /// <summary>
    /// Builds and configures the application service provider.
    /// </summary>
    /// <param name="mbeTableFb">Hardware FB connector.</param>
    /// <returns>Configured service provider.</returns>
    public static IServiceProvider ConfigureServices(MbeTableFB mbeTableFb)
    {
        var logger = new DebugLogger();
        logger.Log("Service configuration started.");

        if (mbeTableFb is null)
        {
            logger.LogException(new ArgumentNullException(nameof(mbeTableFb), "FbConnector cannot be null."));
            throw new ArgumentNullException(nameof(mbeTableFb), "FbConnector cannot be null.");
        }

        var services = new ServiceCollection();

        RegisterConfigurationServices(services);
        RegisterDomainServices(services, logger);

        var appConfiguration = LoadAndValidateAppConfiguration(services, logger);

        RegisterSharedInstances(services, mbeTableFb, appConfiguration);

        RegisterPersistenceServices(services);
        RegisterCommunicationServices(services);
        RegisterUiServices(services);

        logger.Log("Building service provider.");
        var provider = services.BuildServiceProvider();

        InitializeApp(provider);

        logger.Log("Service configuration completed successfully.");
        return provider;
    }

    private static void RegisterConfigurationServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
        services.AddSingleton<IPropertyDefinitionLoader, PropertyDefinitionLoader>();
        services.AddSingleton<IColumnDefsLoader, ColumnDefsLoader>();
        services.AddSingleton<IActionDefsLoader, ActionDefsLoader>();
        services.AddSingleton<IPinGroupDefsLoader, PinGroupDefsLoader>();

        services.AddSingleton<IPropertyDefsValidator, PropertyDefsValidator>();
        services.AddSingleton<IColumnDefsValidator, ColumnDefsValidator>();
        services.AddSingleton<IActionDefsValidator, ActionDefsValidator>();
        services.AddSingleton<IPinGroupDefsValidator, PinGroupDefsValidator>();

        services.AddSingleton<IAppConfigurationValidator, AppConfigurationValidator>();
    }

    private static AppConfiguration LoadAndValidateAppConfiguration(IServiceCollection services, ILogger logger)
    {
        logger.Log("Starting configuration loading and validation.");

        var configDir = Path.Combine(AppContext.BaseDirectory, ConfigFolderName);
        EnsureConfigDirectoryExists(configDir, logger);
        EnsureConfigFilesExist(configDir, DefaultConfigFiles, logger);
        var configFiles = new ConfigFiles(configDir, DefaultConfigFiles);

        var tempProvider = services.BuildServiceProvider();
        var loader = tempProvider.GetRequiredService<IConfigurationLoader>();
        var integrityValidator = tempProvider.GetRequiredService<IAppConfigurationValidator>();

        logger.Log($"Loading configuration from '{configDir}'.");
        var loadResult = loader.LoadConfiguration(configFiles);
        if (loadResult.IsFailed)
        {
            var errorReport = new StringBuilder();
            errorReport.AppendLine("Failed to load configuration due to the following errors:");
            foreach (var error in loadResult.Errors)
            {
                errorReport.AppendLine($" - {error.Message}");
            }
            var ex = new InvalidOperationException(errorReport.ToString());
            logger.LogException(ex, configFiles);
            throw ex;
        }
        var appConfig = loadResult.Value;
        logger.Log("Configuration files loaded and parsed successfully.");

        logger.Log("Validating configuration integrity.");
        var validationResult = integrityValidator.Validate(appConfig);
        if (validationResult.IsFailed)
        {
            var reasons = string.Join(Environment.NewLine + " - ", validationResult.Errors.Select(e => e.Message));
            var errorReport = "Configuration integrity check failed:" + Environment.NewLine + " - " + reasons;
            var ex = new InvalidOperationException(errorReport);
            logger.LogException(ex, appConfig);
            throw ex;
        }
        logger.Log("Configuration integrity check passed.");

        return appConfig;
    }

    private static void EnsureConfigDirectoryExists(string configDir, ILogger logger)
    {
        if (!Directory.Exists(configDir))
        {
            var ex = new InvalidOperationException($"Config directory not found: '{configDir}'.");
            logger.LogException(ex);
            throw ex;
        }
    }

    private static void EnsureConfigFilesExist(string configDir, string[] configFiles, ILogger logger)
    {
        var missing = configFiles
            .Select(f => Path.Combine(configDir, f))
            .Where(fullPath => !File.Exists(fullPath))
            .Select(Path.GetFileName)
            .ToArray();

        if (missing.Length > 0)
        {
            var ex = new InvalidOperationException($"Config files not found in '{configDir}': {string.Join(", ", missing)}");
            logger.LogException(ex);
            throw ex;
        }
    }

    private static void RegisterSharedInstances(IServiceCollection services, MbeTableFB mbeTableFb, AppConfiguration appConfiguration)
    {
        services.AddSingleton(mbeTableFb);
        services.AddSingleton(appConfiguration);
        services.AddSingleton(appConfiguration.Columns);
        services.AddSingleton(appConfiguration.PropertyRegistry);
    }

    private static void RegisterDomainServices(IServiceCollection services, ILogger logger)
    {
        services.AddSingleton(logger);
        services.AddSingleton<IFormulaParser, FormulaParser>();
        services.AddSingleton<ICalculationOrderer, CalculationOrderer>();
        services.AddSingleton<IPlcStateMonitor, PlcStateMonitor>();
        services.AddSingleton<IStatusManager, StatusManager>();
        services.AddSingleton<IActionRepository, ActionRepository>();
        services.AddSingleton<IActionTargetProvider, ActionTargetProvider>();
        services.AddSingleton<IPlcRecipeStatusProvider, PlcRecipeStatusProvider>();
        services.AddSingleton<ICommunicationSettingsProvider, CommunicationSettingsProvider>();
        services.AddSingleton<AppStateMachine>();
        services.AddSingleton<AppStateUiProjector>();
        services.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
        services.AddSingleton<TimerService>();
        services.AddSingleton<IComboboxDataProvider, ComboboxDataProvider>();
        services.AddSingleton<IStepFactory, StepFactory>();
        services.AddSingleton<StepCalculationLogic>();
        services.AddSingleton<StepPropertyCalculator>();
        services.AddSingleton<IRecipeLoopValidator, RecipeLoopValidator>();
        services.AddSingleton<IRecipeTimeCalculator, RecipeTimeCalculator>();
        services.AddSingleton<IRecipeEngine, RecipeEngine>();
        services.AddSingleton<IRecipeApplicationService, RecipeApplicationService>();
        services.AddSingleton<IStepViewModelFactory, StepViewModelFactory>();
        services.AddSingleton<RecipeViewModel>();
        services.AddSingleton<TargetAvailabilityValidator>();
        services.AddSingleton<IPlcRecipeSerializer, PlcRecipeSerializer>();
        services.AddSingleton<IRecipeComparator, RecipeComparatorV1>();
        services.AddSingleton<IRecipePlcSender, RecipePlcSender>();
        services.AddSingleton<PlcCapacityCalculator>();
        services.AddSingleton<IPlcProtocol, PlcProtocol>();
        services.AddSingleton<IModbusTransport, ModbusTransport>();
        services.AddSingleton<RecipeEffectsHandler>();
        services.AddSingleton<IRowExecutionStateProvider, RowExecutionStateProvider>();
        services.AddSingleton<ICellStateResolver, CellStateResolver>();
        services.AddSingleton<ColorScheme>();
    }

    private static void RegisterPersistenceServices(IServiceCollection services)
    {
        services.AddSingleton<IRecipeFileReader, RecipeFileReader>();
        services.AddSingleton<IRecipeFileWriter, RecipeFileWriter>();
        services.AddSingleton<IRecipeSerializer, RecipeCsvSerializer>();
        services.AddSingleton<ICsvHelperFactory, CsvHelperFactory>();
        services.AddSingleton<ICsvStepMapper, CsvStepMapper>();
        services.AddSingleton<ICsvHeaderBinder, CsvHeaderBinder>();
        services.AddSingleton<RecipeFileMetadataSerializer>();
    }

    private static void RegisterCommunicationServices(IServiceCollection services)
    {
        // Already registered in domain services
    }

    private static void RegisterUiServices(IServiceCollection services)
    {
        services.AddSingleton<IColorSchemeProvider, DesignTimeColorSchemeProvider>();
        services.AddSingleton<IComboBoxContext, ComboBoxContext>();

        // File dialogs
        services.AddTransient(_ => new OpenFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            Multiselect = false,
            Title = @"Выберите файл рецепта",
            RestoreDirectory = true
        });

        services.AddTransient(_ => new SaveFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            Title = @"Сохраните файл рецепта",
            RestoreDirectory = true
        });
    }

    private static void InitializeApp(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger>();
        logger.Log("Initializing application state machine and effects handler.");

        var stateMachine = provider.GetRequiredService<AppStateMachine>();
        var effectsHandler = provider.GetRequiredService<RecipeEffectsHandler>();
        stateMachine.InitializeEffects(effectsHandler);

        logger.Log("Application initialization complete.");
    }
}