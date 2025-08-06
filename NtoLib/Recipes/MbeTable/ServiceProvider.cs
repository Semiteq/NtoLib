using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.PinDataManager;
using NtoLib.Recipes.MbeTable.RecipeManager;
using NtoLib.Recipes.MbeTable.RecipeManager.Actions;
using NtoLib.Recipes.MbeTable.RecipeManager.Analysis;
using NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType;
using NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType.Errors;
using NtoLib.Recipes.MbeTable.RecipeManager.StepManager;
using NtoLib.Recipes.MbeTable.RecipeManager.ViewModels;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Status;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable
{
    public class ServiceProvider
    {
        /// <summary>
        /// DI container for the MBE Table application.
        /// </summary>
        public ColorScheme ColorScheme { get; private set; }
        public TableSchema TableSchema { get; private set; }
        public ActionManager ActionManager { get; private set; }
        public RecipeViewModel RecipeViewModel { get; private set; }
        public TableColumnManager TableColumnManager { get; private set; }
        public IStatusManager StatusManager { get; private set; }
        public OpenFileDialog OpenFileDialog { get; private set; }
        public SaveFileDialog SaveFileDialog { get; private set; }
        public Recipe Recipe { get; private set; }
        public StepFactory StepFactory { get; private set; }
        public ModBusDeserializer ModBusDeserializer { get; private set; }
        public PropertyDefinitionRegistry PropertyDefinitionRegistry { get; private set; }
        public MbeTableFB MbeTableFb { get; private set; }
        public IActionTargetProvider ActionTargetProvider { get; private set; }
        public IPlcStateMonitor PlcStateMonitor { get; private set; }
        public ICommunicationSettingsProvider CommunicationSettingsProvider { get; private set; }
        public ComboboxDataProvider ComboboxDataProvider { get; private set; }
        public RecipeEngine RecipeEngine { get; private set; }
        public ActionToFactoryMap ActionToFactoryMap { get; private set; }
        public StepPropertyCalculator StepPropertyCalculator { get; private set; }
        public RecipeLoopValidator RecipeLoopValidator { get; private set; }
        public RecipeTimeCalculator RecipeTimeCalculator { get; private set; }

        public void InitializeServices(MbeTableFB mbeTableFb)
        {
            MbeTableFb = mbeTableFb ?? throw new ArgumentNullException(nameof(mbeTableFb), 
                @"FbConnector cannot be null. Ensure that the VisualFBConnector is properly initialized.");
            
            TableSchema = new TableSchema();
            ActionManager = new ActionManager();
            PropertyDefinitionRegistry = new PropertyDefinitionRegistry();
            
            ActionTargetProvider = new ActionTargetProvider();
            PlcStateMonitor = new PlcStateMonitor();
            StatusManager = new StatusManager();
            ComboboxDataProvider = new ComboboxDataProvider(ActionManager, ActionTargetProvider);
            
            CommunicationSettingsProvider = new CommunicationSettingsProvider(MbeTableFb);
            
            ColorScheme = CreateColorScheme();
            
            StepFactory = new StepFactory(ActionManager, TableSchema, PropertyDefinitionRegistry);
            ModBusDeserializer = new ModBusDeserializer(ActionManager, StepFactory);

            ActionToFactoryMap = new ActionToFactoryMap(ActionManager, StepFactory);

            var actionMap = ActionToFactoryMap.StepCreationMap;

            var dependencyRules = ImmutableList.Create(
                new DependencyRule(
                    TriggerKeys: ImmutableHashSet.Create(ColumnKey.InitialValue, ColumnKey.Setpoint, ColumnKey.Speed),
                    OutputKey: ColumnKey.StepDuration,
                    CalculationFunc: (Func<float, float, float, (float?, CalculationError?)>)StepCalculationLogic.CalculateDurationFromSpeed
                ),
                new DependencyRule(
                    TriggerKeys: ImmutableHashSet.Create(ColumnKey.InitialValue, ColumnKey.Setpoint, ColumnKey.StepDuration),
                    OutputKey: ColumnKey.Speed,
                    CalculationFunc: (Func<float, float, float, (float?, CalculationError?)>)StepCalculationLogic.CalculateSpeedFromDuration
                )
            );
            
            StepPropertyCalculator = new StepPropertyCalculator(dependencyRules);
            RecipeLoopValidator = new RecipeLoopValidator(ActionManager);
            RecipeTimeCalculator = new RecipeTimeCalculator(ActionManager);
            
            RecipeEngine = new RecipeEngine(ActionManager, StepFactory, ActionTargetProvider, actionMap, StepPropertyCalculator);
            RecipeViewModel = new RecipeViewModel(RecipeEngine, RecipeLoopValidator, RecipeTimeCalculator, ComboboxDataProvider, StatusManager, TableSchema);
            
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
            LineTextColor = Color.Black,
            SelectedLineTextColor = Color.Black,
            PassedLineTextColor = Color.DarkGray,
            LineBgColor = Color.White,
            SelectedLineBgColor = Color.Green,
            PassedLineBgColor = Color.Yellow,
            HeaderBgColor = Color.LightGray,
            HeaderTextColor = Color.Black,
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