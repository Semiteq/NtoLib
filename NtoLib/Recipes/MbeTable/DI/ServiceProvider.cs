#nullable enable

using System;
using System.Diagnostics;
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
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table.CellState;
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
        if (mbeTableFb is null)
            throw new ArgumentNullException(nameof(mbeTableFb), "FbConnector cannot be null.");

        var services = new ServiceCollection();

        // Register all application services
        RegisterConfigurationServices(services);
        RegisterDomainServices(services);
        
        // --- Configuration Loading and Validation Sequence ---
        var appConfiguration = LoadAndValidateAppConfiguration(services);

        // --- Register loaded configuration as a singleton instance ---
        RegisterSharedInstances(services, mbeTableFb, appConfiguration);
        
        // Register other services that may depend on the configuration
        RegisterPersistenceServices(services);
        RegisterCommunicationServices(services);
        RegisterUiServices(services);

        var provider = services.BuildServiceProvider();

        InitializeApp(provider);

        return provider;
    }

    private static void RegisterConfigurationServices(IServiceCollection services)
    {
        // Loaders
        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
        services.AddSingleton<IPropertyDefinitionLoader, PropertyDefinitionLoader>();
        services.AddSingleton<IColumnDefsLoader, ColumnDefsLoader>();
        services.AddSingleton<IActionDefsLoader, ActionDefsLoader>();
        services.AddSingleton<IPinGroupDefsLoader, PinGroupDefsLoader>();

        // Individual Validators
        services.AddSingleton<IPropertyDefsValidator, PropertyDefsValidator>();
        services.AddSingleton<IColumnDefsValidator, ColumnDefsValidator>();
        services.AddSingleton<IActionDefsValidator, ActionDefsValidator>();
        services.AddSingleton<IPinGroupDefsValidator, PinGroupDefsValidator>();
        
        // High-Level Integrity Validator
        services.AddSingleton<IAppConfigurationValidator, AppConfigurationValidator>();
    }

    private static AppConfiguration LoadAndValidateAppConfiguration(IServiceCollection services)
    {
        var configDir = Path.Combine(AppContext.BaseDirectory, ConfigFolderName);
        EnsureConfigDirectoryExists(configDir);
        EnsureConfigFilesExist(configDir, DefaultConfigFiles);
        var configFiles = new ConfigFiles(configDir, DefaultConfigFiles);
        
        // Build a temporary provider to get the loader and validator
        var tempProvider = services.BuildServiceProvider();
        var loader = tempProvider.GetRequiredService<IConfigurationLoader>();
        var integrityValidator = tempProvider.GetRequiredService<IAppConfigurationValidator>();

        // 1. Load and perform file-level validation
        var loadResult = loader.LoadConfiguration(configFiles);
        if (loadResult.IsFailed)
        {
            var errorReport = new StringBuilder();
            errorReport.AppendLine("Failed to load configuration due to the following errors:");
            foreach (var error in loadResult.Errors)
            {
                errorReport.AppendLine($" - {error.Message}");
            }
            throw new InvalidOperationException(errorReport.ToString());
        }
        var appConfig = loadResult.Value;
        Debug.Print("Configuration files loaded and parsed successfully.");

        // 2. Perform high-level integrity validation
        var validationResult = integrityValidator.Validate(appConfig);
        if (validationResult.IsFailed)
        {
            var reasons = string.Join(Environment.NewLine + " - ", validationResult.Errors.Select(e => e.Message));
            var errorReport = "Configuration integrity check failed:" + Environment.NewLine + " - " + reasons;
            throw new InvalidOperationException(errorReport);
        }
        Debug.Print("Configuration integrity check passed.");
        
        return appConfig;
    }

    private static void EnsureConfigDirectoryExists(string configDir)
    {
        if (!Directory.Exists(configDir))
            throw new InvalidOperationException($"Config directory not found: '{configDir}'.");
    }

    private static void EnsureConfigFilesExist(string configDir, string[] configFiles)
    {
        var missing = configFiles
            .Select(f => Path.Combine(configDir, f))
            .Where(fullPath => !File.Exists(fullPath))
            .Select(Path.GetFileName)
            .ToArray();

        if (missing.Length > 0)
            throw new InvalidOperationException($"Config files not found in '{configDir}': {string.Join(", ", missing)}");
    }

    private static void RegisterSharedInstances(IServiceCollection services, MbeTableFB mbeTableFb, AppConfiguration appConfiguration)
    {
        services.AddSingleton(mbeTableFb);
        services.AddSingleton(appConfiguration);
        services.AddSingleton(appConfiguration.Columns);
        services.AddSingleton(appConfiguration.PropertyRegistry);
    }
    
    private static void RegisterDomainServices(IServiceCollection services)
    {
        services.AddSingleton<ILogger, DebugLogger>();
        services.AddSingleton<IFormulaParser, FormulaParser>();
        services.AddSingleton<ICalculationOrderer, CalculationOrderer>();
        services.AddSingleton<IPlcStateMonitor, PlcStateMonitor>();
        services.AddSingleton<IStatusManager, StatusManager>();
        services.AddSingleton<IActionRepository, ActionRepository>();
        services.AddSingleton<TableCellStateManager>();
        services.AddSingleton<IActionTargetProvider, ActionTargetProvider>();
        services.AddSingleton<IPlcRecipeStatusProvider, PlcRecipeStatusProvider>();
        services.AddSingleton<ICommunicationSettingsProvider, CommunicationSettingsProvider>();
        services.AddSingleton<AppStateMachine>();
        services.AddSingleton<AppStateUiProjector>();
        services.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
        services.AddSingleton<TimerService>();
        services.AddSingleton<IComboboxDataProvider, ComboboxDataProvider>();
        services.AddSingleton<ICellStylePalette, DefaultCellStylePalette>();
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
        // Protocol and transport are registered in domain services.
    }

    private static void RegisterUiServices(IServiceCollection services)
    {
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
        var stateMachine = provider.GetRequiredService<AppStateMachine>();
        var effectsHandler = provider.GetRequiredService<RecipeEffectsHandler>();
        stateMachine.InitializeEffects(effectsHandler);
    }
}