using System;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Transport;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv.Fingerprints;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table.CellState;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Composition
{
    /// <summary>
    /// DI container for the MBE Table application.
    /// </summary>
    public class ServiceProvider
    {
        // --- Core Services & Managers ---
        public IPlcStateMonitor PlcStateMonitor { get; private set; }
        public IStatusManager StatusManager { get; private set; }
        public ActionManager ActionManager { get; private set; }
        public DebugLogger DebugLogger { get; private set; }
        public ColorScheme ColorScheme { get; private set; }
        public TableCellStateManager TableCellStateManager { get; private set; }
        public IActionTargetProvider ActionTargetProvider { get; private set; }
        public IPlcRecipeStatusProvider PlcRecipeStatusProvider { get; private set; }
        public ICommunicationSettingsProvider CommunicationSettingsProvider { get; private set; }
        public MbeTableFB MbeTableFb { get; private set; }

        // --- Data & Schema ---
        public TableSchema TableSchema { get; private set; }
        public PropertyDefinitionRegistry PropertyDefinitionRegistry { get; private set; }
        public DependencyRulesMap DependencyRulesMap { get; private set; }
        public IComboboxDataProvider ComboboxDataProvider { get; private set; }

        // --- Factories & Maps ---
        public StepFactory StepFactory { get; private set; }
        public TableColumnFactoryMap TableColumnFactoryMap { get; private set; }

        // --- Analysis & Engine ---
        public StepPropertyCalculator StepPropertyCalculator { get; private set; }
        public RecipeLoopValidator RecipeLoopValidator { get; private set; }
        public RecipeTimeCalculator RecipeTimeCalculator { get; private set; }
        public IRecipeEngine RecipeEngine { get; private set; }
        public StepCalculationLogic StepCalculationLogic { get; private set; }

        // --- ViewModels ---
        public RecipeViewModel RecipeViewModel { get; private set; }

        // --- IO ---
        public RecipeFileReader RecipeFileReader { get; private set; }
        public RecipeFileWriter RecipeFileWriter { get; private set; }
        public RecipeCsvSerializerV1 RecipeCsvSerializerV1 { get; private set; }
        public ICsvHelperFactory CsvHelperFactory { get; private set; }
        public ICsvStepMapper CsvStepMapper { get; private set; }
        public ICsvHeaderBinder CsvHeaderBinder { get; private set; }
        public RecipeFileMetadataSerializer RecipeFileMetadataSerializer { get; private set; }
        public SchemaFingerprintUtil SchemaFingerprintUtil { get; private set; }
        public IActionsFingerprintUtil ActionsFingerprintUtil { get; private set; }
        public TargetAvailabilityValidator TargetAvailabilityValidator { get; private set; }
        
        // --- Modbus ---
        public IPlcRecipeSerializer PlcRecipeSerializer { get; private set; }
        public IRecipeComparator RecipeComparator { get; private set; }
        public IRecipePlcSender RecipePlcSender { get; private set; }
        public PlcCapacityCalculator PlcCapacityCalculator { get; private set; }
        public IPlcProtocol PlcProtocol { get; private set; }
        public IModbusTransport ModbusTransport { get; private set; }

        // --- UI Components ---
        public OpenFileDialog OpenFileDialog { get; private set; }
        public SaveFileDialog SaveFileDialog { get; private set; }

        public void InitializeServices(MbeTableFB mbeTableFb)
        {
            // --- External mandatory dependency ---
            MbeTableFb = mbeTableFb ?? throw new ArgumentNullException(nameof(mbeTableFb),
                @"FbConnector cannot be null. Ensure that the VisualFBConnector is properly initialized.");

            // --- Independent core services & registries ---
            TableSchema = new TableSchema();
            ActionManager = new ActionManager();
            PropertyDefinitionRegistry = new PropertyDefinitionRegistry();
            StepCalculationLogic = new StepCalculationLogic();
            DependencyRulesMap = new DependencyRulesMap(StepCalculationLogic);
            ActionTargetProvider = new ActionTargetProvider();
            PlcStateMonitor = new PlcStateMonitor();
            StatusManager = new StatusManager();
            DebugLogger = new DebugLogger();
            ColorScheme = CreateColorScheme();

            // --- Services with simple dependencies ---
            CommunicationSettingsProvider = new CommunicationSettingsProvider(MbeTableFb);
            ComboboxDataProvider = new ComboboxDataProvider(ActionManager, ActionTargetProvider);
            PlcRecipeStatusProvider = new PlcRecipeStatusProvider();
            TableCellStateManager = new TableCellStateManager(PlcRecipeStatusProvider,ColorScheme);
            TableColumnFactoryMap = new TableColumnFactoryMap(ComboboxDataProvider);
            StepFactory = new StepFactory(ActionManager, PropertyDefinitionRegistry, TableSchema);

            // --- Analysis services ---
            var dependencyRules = DependencyRulesMap.GetMap;
            StepPropertyCalculator = new StepPropertyCalculator(dependencyRules);
            RecipeLoopValidator = new RecipeLoopValidator(ActionManager, DebugLogger);
            RecipeTimeCalculator = new RecipeTimeCalculator(ActionManager);

            // --- Core engine ---
            RecipeEngine = new RecipeEngine(
                ActionManager,
                StepFactory,
                ActionTargetProvider,
                StepPropertyCalculator,
                DebugLogger);

            // --- IO ---
            CsvHelperFactory = new CsvHelperFactory();
            CsvHeaderBinder = new CsvHeaderBinder();
            RecipeFileMetadataSerializer = new RecipeFileMetadataSerializer();
            SchemaFingerprintUtil = new SchemaFingerprintUtil();
            ActionsFingerprintUtil = new ActionsFingerprintUtil();
            TargetAvailabilityValidator = new TargetAvailabilityValidator();

            CsvStepMapper = new CsvStepMapper(StepFactory, ActionManager);

            RecipeCsvSerializerV1 = new RecipeCsvSerializerV1(
                TableSchema,
                ActionManager,
                CsvHelperFactory,
                RecipeFileMetadataSerializer,
                SchemaFingerprintUtil,
                ActionsFingerprintUtil,
                CsvHeaderBinder,
                CsvStepMapper,
                RecipeLoopValidator,
                TargetAvailabilityValidator,
                ActionTargetProvider
            );

            RecipeFileReader = new RecipeFileReader(RecipeCsvSerializerV1);
            RecipeFileWriter = new RecipeFileWriter(RecipeCsvSerializerV1);
            
            // --- Modbus ---
            PlcCapacityCalculator = new PlcCapacityCalculator();
            
            ModbusTransport = new ModbusTransportV1(CommunicationSettingsProvider, DebugLogger);
            PlcProtocol = new PlcProtocolV1(ModbusTransport, CommunicationSettingsProvider, DebugLogger);
            PlcRecipeSerializer = new PlcRecipeSerializerV1(StepFactory, ActionManager, CommunicationSettingsProvider);
            RecipeComparator = new RecipeComparatorV1(DebugLogger, CommunicationSettingsProvider);

            RecipePlcSender = new RecipePlcSender(
                PlcProtocol, 
                PlcRecipeSerializer, 
                RecipeComparator,
                PlcCapacityCalculator, 
                CommunicationSettingsProvider, 
                DebugLogger
            );
            
            // --- ViewModels ---
            RecipeViewModel = new RecipeViewModel(
                RecipeEngine,
                RecipeFileWriter,
                RecipeFileReader,
                RecipePlcSender,
                RecipeLoopValidator,
                RecipeTimeCalculator,
                ComboboxDataProvider,
                StatusManager,
                DebugLogger
            );

            // --- UI Components ---
            InitializeUiComponents();
        }

        private void InitializeUiComponents()
        {
            OpenFileDialog = CreateOpenFileDialog();
            SaveFileDialog = CreateSaveFileDialog();
        }

        private ColorScheme CreateColorScheme() => new ColorScheme
        {
            ControlBackgroundColor = Color.White,
            TableBackgroundColor = Color.White,

            HeaderFont = new Font("Arial", 16f, FontStyle.Bold),
            LineFont = new Font("Arial", 14f),
            SelectedLineFont = new Font("Arial", 14f),
            PassedLineFont = new Font("Arial", 14f),
            BlockedFont = new Font("Arial", 14f),

            LineTextColor = Color.Black,
            SelectedLineTextColor = Color.Black,
            PassedLineTextColor = Color.DarkGray,
            BlockedTextColor = Color.DarkGray,

            LineBgColor = Color.White,
            SelectedLineBgColor = Color.Green,
            PassedLineBgColor = Color.Yellow,
            HeaderBgColor = Color.LightGray,
            HeaderTextColor = Color.Black,
            BlockedBgColor = Color.LightGray,

            ButtonsColor = Color.LightGray,
        };

        private OpenFileDialog CreateOpenFileDialog() => new OpenFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            Multiselect = false,
            Title = @"Выберите файл рецепта",
            RestoreDirectory = true
        };

        private SaveFileDialog CreateSaveFileDialog() => new SaveFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            Title = @"Сохраните файл рецепта",
            RestoreDirectory = true
        };
    }
}