using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Composition
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
        public IStatusManager StatusManager { get; private set; }
        public OpenFileDialog OpenFileDialog { get; private set; }
        public SaveFileDialog SaveFileDialog { get; private set; }
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
        public DependencyRulesMap DependencyRulesMap { get; private set; }
        public DebugLogger DebugLogger { get; private set; }
        public TableColumnFactoryMap TableColumnFactoryMap { get; private set; }
        
        public void InitializeServices(MbeTableFB mbeTableFb)
        {
            MbeTableFb = mbeTableFb ?? throw new ArgumentNullException(nameof(mbeTableFb), 
                @"FbConnector cannot be null. Ensure that the VisualFBConnector is properly initialized.");
            
            TableSchema = new TableSchema();
            ActionManager = new ActionManager();
            PropertyDefinitionRegistry = new PropertyDefinitionRegistry();
            DependencyRulesMap = new DependencyRulesMap();
            ActionTargetProvider = new ActionTargetProvider();
            PlcStateMonitor = new PlcStateMonitor();
            StatusManager = new StatusManager();
            DebugLogger = new DebugLogger();
            
            ComboboxDataProvider = new ComboboxDataProvider(ActionManager, ActionTargetProvider);
            
            CommunicationSettingsProvider = new CommunicationSettingsProvider(MbeTableFb);
            
            ColorScheme = CreateColorScheme();

            TableColumnFactoryMap = new TableColumnFactoryMap(ComboboxDataProvider);
            StepFactory = new StepFactory(ActionManager, TableSchema, PropertyDefinitionRegistry);
            ModBusDeserializer = new ModBusDeserializer(ActionManager, StepFactory);

            ActionToFactoryMap = new ActionToFactoryMap(ActionManager, StepFactory);
            
            var actionMap = ActionToFactoryMap.GetMap;
            var dependencyRules = DependencyRulesMap.GetMap;
            
            StepPropertyCalculator = new StepPropertyCalculator(dependencyRules);
            RecipeLoopValidator = new RecipeLoopValidator(ActionManager);
            RecipeTimeCalculator = new RecipeTimeCalculator(ActionManager);
            
            RecipeEngine = new RecipeEngine(
                ActionManager, 
                StepFactory, 
                ActionTargetProvider, 
                actionMap, 
                StepPropertyCalculator,
                DebugLogger);
            
            RecipeViewModel = new RecipeViewModel(
                RecipeEngine, 
                RecipeLoopValidator, 
                RecipeTimeCalculator, 
                ComboboxDataProvider, 
                StatusManager, 
                TableSchema,
                DebugLogger,
                OpenFileDialog,
                SaveFileDialog);
            
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