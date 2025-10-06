

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable;

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
    private const string ConfigFolderName = "NtoLibTableConfig";
    private const string PropertyDefsFileName = "PropertyDefs.yaml";
    private const string ColumnDefsFileName = "ColumnDefs.yaml";
    private const string PinGroupDefsFileName = "PinGroupDefs.yaml";
    private const string ActionsDefsFileName = "ActionsDefs.yaml";

    public IServiceProvider? ServiceProvider => _serviceProvider;

    [NonSerialized] private Lazy<ConfigurationState>? _configurationStateLazy;
    [NonSerialized] private readonly object _configurationLock = new();
    [NonSerialized] private IServiceProvider? _serviceProvider;
    [NonSerialized] private TimerService? _timerService;
    [NonSerialized] private IRecipeRuntimeState? _runtimeState;
    [NonSerialized] private IActionTargetProvider? _actionTargetProvider;
    
    protected override void ToDesign()
    {
        base.ToDesign();
        CleanupServices();
    }
    
    protected override void ToRuntime()
    {
        base.ToRuntime();
        
        var state = EnsureConfigurationLoaded();
        InitializeServices(state);
    }
    
    public override void Dispose()
    {
        CleanupServices();
        base.Dispose();
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        if (_runtimeState == null || _timerService == null)
        {
            return;
        }

        _runtimeState.Poll();
        _timerService.UpdateFromSnapshot(_runtimeState.Current);

        UpdateUiConnectionPins();
    }
}