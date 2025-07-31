using System;
using System.Drawing;
using System.Windows.Forms;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.ActionTargets;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
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
        public TableCellFormatter TableCellFormatter { get; private set; }
        public ComboBoxDataProvider DataProvider { get; private set; }
        public RecipeViewModel RecipeViewModel { get; private set; }
        public TableColumnManager TableColumnManager { get; private set; }
        public TablePainter TablePainter { get; private set; }
        public IStatusManager StatusManager { get; private set; }
        public OpenFileDialog OpenFileDialog { get; private set; }
        public SaveFileDialog SaveFileDialog { get; private set; }
        public RecipeManager RecipeManager { get; private set; }
        public PropertyDependencyCalc PropertyDependencyCalc { get; private set; }
        public StepFactory StepFactory { get; private set; }
        public ModBusDeserializer ModBusDeserializer { get; private set; }
        public PropertyDefinitionRegistry PropertyDefinitionRegistry { get; private set; }
        public VisualFBConnector FbConnector { get; private set; }

        public void InitializeServices(VisualFBConnector fbConnector)
        {
            FbConnector = fbConnector ?? throw new ArgumentNullException(nameof(fbConnector), 
                @"FbConnector cannot be null. Ensure that the VisualFBConnector is properly initialized.");
            
            TableSchema = new TableSchema();
            ActionManager = new ActionManager();
            TableCellFormatter = new TableCellFormatter();
            PropertyDefinitionRegistry = new PropertyDefinitionRegistry();
            ColorScheme = CreateColorScheme();

            StatusManager = new StatusManager();
            TablePainter = new TablePainter(ColorScheme);
            
            if (FbConnector.Fb is not IFbActionTarget fbTarget)
                throw new InvalidOperationException("FbConnector.Fb must implement IFbActionTarget.");
            
            PropertyDependencyCalc = new PropertyDependencyCalc(ActionManager, TableSchema);
            StepFactory = new StepFactory(ActionManager, TableSchema, PropertyDefinitionRegistry);
            DataProvider = new ComboBoxDataProvider(ActionManager, fbTarget);
            ModBusDeserializer = new ModBusDeserializer(ActionManager, StepFactory);
            
            RecipeManager = new RecipeManager(TableSchema, PropertyDependencyCalc, StepFactory);
            RecipeViewModel = new RecipeViewModel(RecipeManager, DataProvider);
            
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