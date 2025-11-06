using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using FB;
using FB.VisualFB;

using InSAT.Library.Interop;
using InSAT.OPC;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure;

namespace NtoLib.Recipes.MbeTable
{
    /// <summary>
    /// MBE recipe table function block for MasterSCADA 3.12.
    /// Manages recipe execution and communication with PLC.
    /// </summary>
    [CatID(CatIDs.CATID_OTHER)]
    [Guid("DFB05172-07CD-492C-925E-A091B197D8A8")]
    [FBOptions(FBOptions.EnableChangeConfigInRT)]
    [VisualControls(typeof(TableControl))]
    [DisplayName("Таблица рецептов MBE")]
    [ComVisible(true)]
    [Serializable]
    public partial class MbeTableFB : VisualFBBase
    {
        private const string DefaultConfigFolderName = "NtoLibTableConfig";
        private const string DefaultPropertyDefsFileName = "PropertyDefs.yaml";
        private const string DefaultColumnDefsFileName = "ColumnDefs.yaml";
        private const string DefaultPinGroupDefsFileName = "PinGroupDefs.yaml";
        private const string DefaultActionsDefsFileName = "ActionsDefs.yaml";

        public IServiceProvider? ServiceProvider => _serviceProvider;

        [NonSerialized] private Lazy<AppConfiguration>? _appConfigurationLazy;
        [NonSerialized] private IServiceProvider? _serviceProvider;
        [NonSerialized] private RuntimeServiceHost? _runtimeServiceHost;
        [NonSerialized] private IReadOnlyDictionary<short, CompiledFormula>? _compiledFormulas;

        protected override void ToDesign()
        {
            base.ToDesign();
            CleanupRuntime();
        }

        protected override void ToRuntime()
        {
            base.ToRuntime();
            InitializeRuntime();
        }

        public override void Dispose()
        {
            CleanupRuntime();
            base.Dispose();
        }

        private void InitializeRuntime()
        {
            if (_serviceProvider != null)
            {
                return;
            }

            try
            {
                var state = EnsureConfigurationLoaded();

                if (_compiledFormulas == null)
                    throw new InvalidOperationException("Compiled formulas cache was not initialized.");

                _serviceProvider = MbeTableServiceConfigurator.ConfigureServices(this, state, _compiledFormulas);
                _runtimeServiceHost = new RuntimeServiceHost(_serviceProvider);
            }
            catch (Exception ex)
            {
                var fullMessage =
                    $"Service initialization failed:\n\n{ex.GetType().Name}: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}";

                if (ex.InnerException != null)
                {
                    fullMessage +=
                        $"\n\nInner Exception:\n{ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
                }

                MessageBox.Show(fullMessage, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void CleanupRuntime()
        {
            if (_serviceProvider == null)
            {
                return;
            }

            _runtimeServiceHost?.Dispose();
            _runtimeServiceHost = null;

            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }

            _serviceProvider = null;
        }

        protected override void UpdateData()
        {
            base.UpdateData();

            _runtimeServiceHost?.Poll();

            UpdateUiConnectionPins();
        }

        /// <summary>
        /// Updates timer-related pins. Called by RuntimeServiceHost.
        /// </summary>
        internal void UpdateTimerPins(TimeSpan stepTimeLeft, TimeSpan totalTimeLeft)
        {
            if (GetPinQuality(IdLineTimeLeft) != OpcQuality.Good
                || !AreFloatsEqual(GetPinValue<float>(IdLineTimeLeft), (float)stepTimeLeft.TotalSeconds))
            {
                SetPinValue(IdLineTimeLeft, (float)stepTimeLeft.TotalSeconds);
            }

            if (GetPinQuality(IdTotalTimeLeft) != OpcQuality.Good
                || !AreFloatsEqual(GetPinValue<float>(IdTotalTimeLeft), (float)totalTimeLeft.TotalSeconds))
            {
                SetPinValue(IdTotalTimeLeft, (float)totalTimeLeft.TotalSeconds);
            }
        }
    }
}