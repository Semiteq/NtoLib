// MbeTableServiceConfigurator.cs

using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Loaders;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Application.Services;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis.Rules;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
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
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;
using NtoLib.Recipes.MbeTable.StateMachine;
using NtoLib.Recipes.MbeTable.StateMachine.App;
using NtoLib.Recipes.MbeTable.StateMachine.Contracts;
using NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

namespace NtoLib.Recipes.MbeTable.DI;

/// <summary>
/// Configures the dependency injection container for the MBE Table application.
/// Follows the composition root pattern to register all application services.
/// </summary>
public static class MbeTableServiceConfigurator
{
    /// <summary>
    /// Creates and configures an <see cref="IServiceProvider"/> with all necessary services for the application.
    /// </summary>
    /// <param name="mbeTableFb">The mandatory, externally provided MbeTableFB connector instance.</param>
    /// <returns>A fully configured <see cref="IServiceProvider"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="mbeTableFb"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if application configuration cannot be loaded.</exception>
    public static IServiceProvider ConfigureServices(MbeTableFB mbeTableFb)
    {
        if (mbeTableFb is null)
        {
            throw new ArgumentNullException(nameof(mbeTableFb),
                "FbConnector cannot be null. Ensure that the VisualFBConnector is properly initialized.");
        }

        var services = new ServiceCollection();

        // --- External Dependencies & Configuration ---
        services.AddSingleton(mbeTableFb);

        var configurationLoader = new ConfigurationLoader(
            new TableSchemaLoader(), 
            new ActionsLoader()
        );
        
        var configResult = configurationLoader.LoadConfiguration(
            AppDomain.CurrentDomain.BaseDirectory,
            "TableSchema.json",
            "ActionSchema.json"
        );

        if (configResult.IsFailed)
        {
            var errorMessage = $"Failed to load application configuration. Reasons: {string.Join("; ", configResult.Errors.Select(e => e.Message))}";
            throw new InvalidOperationException(errorMessage);
        }
        
        var appConfiguration = configResult.Value;
        services.AddSingleton(appConfiguration);
        services.AddSingleton(appConfiguration.Schema); // Register schema separately for convenience.

        // --- Core Services & Managers ---
        services.AddSingleton<IPlcStateMonitor, PlcStateMonitor>();
        services.AddSingleton<IStatusManager, StatusManager>();
        services.AddSingleton<IActionRepository, ActionRepository>();
        services.AddSingleton<ILogger, DebugLogger>();
        services.AddSingleton<TableCellStateManager>();
        services.AddSingleton<IActionTargetProvider, ActionTargetProvider>();
        services.AddSingleton<IPlcRecipeStatusProvider, PlcRecipeStatusProvider>();
        services.AddSingleton<ICommunicationSettingsProvider, CommunicationSettingsProvider>();
        services.AddSingleton<AppStateMachine>();
        services.AddSingleton<AppStateUiProjector>();
        services.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
        services.AddSingleton<TimerService>();

        // --- Data & Schema ---
        services.AddSingleton<PropertyDefinitionRegistry>();
        services.AddSingleton<IComboboxDataProvider, ComboboxDataProvider>();
        services.AddSingleton<ITableSchemaLoader, TableSchemaLoader>();

        // --- Factories & Maps ---
        services.AddSingleton<IStepFactory, StepFactory>();

        // --- Analysis & Engine ---
        services.AddSingleton<StepCalculationLogic>();
        services.AddSingleton<ICalculationRule, SmoothRampCalculationRule>();
        services.AddSingleton<StepPropertyCalculator>();
        services.AddSingleton<IRecipeLoopValidator, RecipeLoopValidator>();
        services.AddSingleton<IRecipeTimeCalculator, RecipeTimeCalculator>();
        services.AddSingleton<IRecipeEngine, RecipeEngine>();

        // --- ViewModels ---
        services.AddSingleton<IRecipeApplicationService, RecipeApplicationService>();
        services.AddSingleton<IStepViewModelFactory, StepViewModelFactory>();
        services.AddSingleton<RecipeViewModel>();

        // --- IO ---
        services.AddSingleton<IRecipeFileReader, RecipeFileReader>();
        services.AddSingleton<IRecipeFileWriter, RecipeFileWriter>();
        services.AddSingleton<IRecipeSerializer, RecipeCsvSerializer>();
        services.AddSingleton<ICsvHelperFactory, CsvHelperFactory>();
        services.AddSingleton<ICsvStepMapper, CsvStepMapper>();
        services.AddSingleton<ICsvHeaderBinder, CsvHeaderBinder>();
        services.AddSingleton<RecipeFileMetadataSerializer>();
        services.AddSingleton<TargetAvailabilityValidator>();

        // --- Modbus ---
        services.AddSingleton<IPlcRecipeSerializer, PlcRecipeSerializerV1>();
        services.AddSingleton<IRecipeComparator, RecipeComparatorV1>();
        services.AddSingleton<IRecipePlcSender, RecipePlcSender>();
        services.AddSingleton<PlcCapacityCalculator>();
        services.AddSingleton<IPlcProtocol, PlcProtocolV1>();
        services.AddSingleton<IModbusTransport, ModbusTransport>();

        // --- UI Components & Handlers ---
        services.AddSingleton<RecipeEffectsHandler>();

        // A new ColorScheme instance can be created and configured separately.
        // It can be added to the container if needed.
        // services.AddSingleton(new ColorScheme());

        // UI dialogs should be created on demand (Transient lifestyle).
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

        var serviceProvider = services.BuildServiceProvider();

        // --- Post-configuration steps ---
        // Some services need to be initialized after they are created.
        var stateMachine = serviceProvider.GetRequiredService<AppStateMachine>();
        var effectsHandler = serviceProvider.GetRequiredService<RecipeEffectsHandler>();
        stateMachine.InitializeEffects(effectsHandler);

        return serviceProvider;
    }
}