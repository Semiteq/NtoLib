#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using MasterSCADA.Hlp;
using Microsoft.Extensions.DependencyInjection;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.DI;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable;

[CatID(CatIDs.CATID_OTHER)]
[Guid("DFB05172-07CD-492C-925E-A091B197D8A8")]
[FBOptions(FBOptions.EnableChangeConfigInRT)]
[VisualControls(typeof(TableControl))]
[DisplayName("Таблица рецептов MBE")]
[ComVisible(true)]
[Serializable]
public class MbeTableFB : VisualFBBase
{
    /// <summary>
    /// Gets the service provider instance.
    /// It is initialized in ToRuntime and cleared in ToDesign.
    /// Will be null in Design-Time mode.
    /// </summary>
    public IServiceProvider? ServiceProvider => _serviceProvider;

    [NonSerialized] private IServiceProvider? _serviceProvider; 
    [NonSerialized] private TimerService _timerService;
    [NonSerialized] private IPlcStateMonitor _plcStateMonitor;
    [NonSerialized] private IActionTargetProvider _actionTargetProvider;
    [NonSerialized] private IPlcRecipeStatusProvider _plcRecipeStatusProvider;

    #region Numerical Properties

    // Pin ID's
    private const int IdRecipeActive = 1;
    private const int IdCurrentLine = 3;
    private const int IdStepCurrentTime = 4;
    private const int IdForLoopCount1 = 5;
    private const int IdForLoopCount2 = 6;
    private const int IdForLoopCount3 = 7;
    private const int IdEnaSend = 8;
    private const int IdTotalTimeLeft = 101;
    private const int IdLineTimeLeft = 102;

    // ActionProperty pin ID's
    private const int ShutterNamesGroupId = 200;
    private const int HeaterNamesGroupId = 300;
    private const int NitrogenSourcesGroupId = 400;

    private const int IdFirstShutterName = 201;
    private const int IdFirstHeaterName = 301;
    private const int IdFirstNitrogenSourceName = 401;

    private const int ShutterNameQuantity = 32;
    private const int HeaterNameQuantity = 32;
    private const int NitrogenSourceNameQuantity = 3;

    // Communication pin ID's
    private const int IdHmiFloatBaseAddr = 1003;
    private const int IdHmiFloatAreaSize = 1004;
    private const int IdHmiIntBaseAddr = 1005;
    private const int IdHmiIntAreaSize = 1006;
    private const int IdHmiBoolBaseAddr = 1007;
    private const int IdHmiBoolAreaSize = 1008;
    private const int IdHmiControlBaseAddr = 1009;
    private const int IdHmiIp1 = 1010;
    private const int IdHmiIp2 = 1011;
    private const int IdHmiIp3 = 1012;
    private const int IdHmiIp4 = 1013;
    private const int IdHmiPort = 1014;

    // Default values
    private uint _floatBaseAddr = 8100;
    private uint _floatAreaSize = 19600;

    private uint _intBaseAddr = 27700;
    private uint _intAreaSize = 1400;

    private uint _boolBaseAddr = 29100;
    private uint _boolAreaSize = 50;

    private uint _controlBaseAddr = 8000;

    private uint _controllerIp1 = 192;
    private uint _controllerIp2 = 168;
    private uint _controllerIp3 = 0;
    private uint _controllerIp4 = 141;

    private uint _controllerTcpPort = 502;
    #endregion

    #region VisualProperties

    /// !!! ATTENTION !!!
    /// It is strictly forbidden to change names or types of the following properties (at least the public ones).
    /// SCADA caches Pin's and Pout's. In case a project already contained an old version of the FB,
    /// cached data and dll data will be different, so the FB will break and corrupt the project.
    /// If necessary, change the following at your own risk and make a backup of the SCADA project.
    /// The same may apply to MbeTableFb.xml (not tested).
    /// Consider this as a legacy code.

    [Description("Определяет начальный адрес, куда помещаются данные типа 'вещественный'")]
    [DisplayName(" 1.  Базовый адрес хранения данных типа Real (Float)")]
    public uint UFloatBaseAddr
    {
        get => _floatBaseAddr;
        set => _floatBaseAddr = value;
    }

    [Description("Определяет размер области для данных типа 'вещественный'.")]
    [DisplayName(" 2.  Размер области хранения данных типа Real (Float)")]
    public uint UFloatAreaSize
    {
        get => _floatAreaSize;
        set => _floatAreaSize = value;
    }

    [Description("Определяет начальный адрес, куда помещаются данные типа 'целый 16 бит'")]
    [DisplayName(" 3.  Базовый адрес хранения данных типа Int")]
    public uint UIntBaseAddr
    {
        get => _intBaseAddr;
        set => _intBaseAddr = value;
    }

    [DisplayName(" 4.  Размер области хранения данных типа Int")]
    [Description("Определяет размер области для данных типа 'целый 16 бит'")]
    public uint UIntAreaSize
    {
        get => _intAreaSize;
        set => _intAreaSize = value;
    }

    [DisplayName(" 5.  Базовый адрес хранения данных типа Boolean")]
    [Description("Определяет начальный адрес, куда помещаются данные типа 'логический'.")]
    public uint UBoolBaseAddr
    {
        get => _boolBaseAddr;
        set => _boolBaseAddr = value;
    }

    [Description("Определяет размер области для данных типа 'логический'.")]
    [DisplayName(" 6.  Размер области хранения данных типа Boolean")]
    public uint UBoolAreaSize
    {
        get => _boolAreaSize;
        set => _boolAreaSize = value;
    }

    [DisplayName(" 7.  Базовый адрес контрольной области")]
    [Description("Определяет начальный адрес, где располагается зона контрольных данных (3 слова)")]
    public uint UControlBaseAddr
    {
        get => _controlBaseAddr;
        set => _controlBaseAddr = value;
    }

    [Description("IP адрес контроллера байт 1")]
    [DisplayName("8.  IP адрес контроллера байт 1")]
    public uint UControllerIp1
    {
        get => _controllerIp1;
        set => _controllerIp1 = value;
    }

    [DisplayName("9.  IP адрес контроллера байт 2")]
    [Description("IP адрес контроллера байт 2")]
    public uint UControllerIp2
    {
        get => _controllerIp2;
        set => _controllerIp2 = value;
    }

    [Description("IP адрес контроллера байт 3")]
    [DisplayName("10.  IP адрес контроллера байт 3")]
    public uint UControllerIp3
    {
        get => _controllerIp3;
        set => _controllerIp3 = value;
    }

    [Description("IP адрес контроллера байт 4")]
    [DisplayName("11.  IP адрес контроллера байт 4")]
    public uint UControllerIp4
    {
        get => _controllerIp4;
        set => _controllerIp4 = value;
    }

    [DisplayName("12.  TCP порт")]
    [Description("TCP порт")]
    public uint ControllerTcpPort
    {
        get => _controllerTcpPort;
        set => _controllerTcpPort = value;
    }

    #endregion

    public Dictionary<int, string> GetShutterNames() => ReadPinGroup(IdFirstShutterName, ShutterNameQuantity);
    public Dictionary<int, string> GetHeaterNames() => ReadPinGroup(IdFirstHeaterName, HeaterNameQuantity);
    public Dictionary<int, string> GetNitrogenSourceNames() => ReadPinGroup(IdFirstNitrogenSourceName, NitrogenSourceNameQuantity);

    protected override void UpdateData()
    {
        base.UpdateData();

        // Service fields are null in design-time, so we must guard against it.
        if (_plcStateMonitor == null || _plcRecipeStatusProvider == null || _timerService == null)
        {
            return;
        }

        var stepCurrentTime = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<float>(IdStepCurrentTime) : -1f;
        var lineNumber = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdCurrentLine) : -1;
        var forLoopCount1 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount1) : -1;
        var forLoopCount2 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount2) : -1;
        var forLoopCount3 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount3) : -1;

        _plcStateMonitor.UpdateState(lineNumber, forLoopCount1, forLoopCount2, forLoopCount3, stepCurrentTime);

        // For safety reason if failed to read, then consider the recipe is running
        var isRecipeActive = GetPinQuality(IdRecipeActive) is OpcQuality.Good && GetPinValue<bool>(IdRecipeActive);
        var curentLine = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdCurrentLine) : -1;
        var isEnaSend = GetPinQuality(IdEnaSend) is OpcQuality.Good && GetPinValue<bool>(IdEnaSend);

        _plcRecipeStatusProvider.UpdateStatus(isRecipeActive, isEnaSend, curentLine);

        _timerService.Update();

        UpdateUiConnectionPins();
    }

    protected override void ToDesign()
    {
        base.ToDesign();
        CleanupServices();
    }

    protected override void ToRuntime()
    {
        base.ToRuntime();
        InitializeServices();
    }

    public override void Dispose()
    {
        CleanupServices();
        base.Dispose();
    }

    private void OnTimesUpdated(TimeSpan stepTimeLeft, TimeSpan totalTimeLeft)
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

    protected override void CreatePinMap(bool newObject)
    {
        base.CreatePinMap(newObject);

        var shutterGroup = Root.AddGroup(ShutterNamesGroupId, "ShutterNames");
        var heaterGroup = Root.AddGroup(HeaterNamesGroupId, "HeaterNames");
        var nitrogenGroup = Root.AddGroup(NitrogenSourcesGroupId, "NitrogenSourcesNames");

        for (var i = 0; i < ShutterNameQuantity; i++)
        {
            var pinId = IdFirstShutterName + i;
            var pinName = $"Shutter{i + 1}";
            shutterGroup.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), 0d);
        }

        for (var i = 0; i < HeaterNameQuantity; i++)
        {
            var pinId = IdFirstHeaterName + i;
            var pinName = $"Heater{i + 1}";
            heaterGroup.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), 0d);
        }

        for (var i = 0; i < NitrogenSourceNameQuantity; i++)
        {
            var pinId = IdFirstNitrogenSourceName + i;
            var pinName = $"NitrogenSource{i + 1}";
            nitrogenGroup.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), 0d);
        }

        FirePinSpaceChanged();
    }

    private void InitializeServices()
    {
        // Avoid re-initialization if already in runtime.
        if (_serviceProvider != null) return;

        // This is the single point of entry for service creation.
        // We call the static configurator to build the service provider.
        _serviceProvider = MbeTableServiceConfigurator.ConfigureServices(this);

        // Get services from the container.
        var debugLogger = _serviceProvider.GetRequiredService<ILogger>();
        debugLogger.Log("MbeTableFB: Entering Runtime. ServiceProvider created via DI container.");

        _timerService = _serviceProvider.GetRequiredService<TimerService>();
        _plcStateMonitor = _serviceProvider.GetRequiredService<IPlcStateMonitor>();
        _plcRecipeStatusProvider = _serviceProvider.GetRequiredService<IPlcRecipeStatusProvider>();
        _actionTargetProvider = _serviceProvider.GetRequiredService<IActionTargetProvider>();

        _actionTargetProvider.RefreshTargets();
        _timerService.TimesUpdated += OnTimesUpdated;

        debugLogger.Log("MbeTableFB: Runtime services initialized and event handlers subscribed.");
    }

    /// <summary>
    /// Symmetrically cleans up all runtime services and subscriptions.
    /// This method is safe to call multiple times.
    /// </summary>
    private void CleanupServices()
    {
        if (_serviceProvider == null) return; // Already cleaned up.

        var debugLogger = _serviceProvider.GetService<ILogger>(); // Use GetService to avoid exception if already disposed
        debugLogger?.Log("MbeTableFB: Entering Design mode or Disposing. Cleaning up services.");

        if (_timerService != null)
        {
            _timerService.TimesUpdated -= OnTimesUpdated;
        }

        // The service provider from Microsoft.Extensions.DependencyInjection implements IDisposable.
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }

        // Nullify all references to ensure garbage collection and prevent accidental use.
        _plcStateMonitor = null;
        _actionTargetProvider = null;
        _plcRecipeStatusProvider = null;
        _timerService = null;
        _serviceProvider = null;
    }

    private void UpdateUiConnectionPins()
    {
        VisualPins.SetValue<uint>(IdHmiFloatBaseAddr, UFloatBaseAddr);
        VisualPins.SetValue<uint>(IdHmiFloatAreaSize, UFloatAreaSize);
        VisualPins.SetValue<uint>(IdHmiIntBaseAddr, UIntBaseAddr);
        VisualPins.SetValue<uint>(IdHmiIntAreaSize, UIntAreaSize);
        VisualPins.SetValue<uint>(IdHmiBoolBaseAddr, UBoolBaseAddr);
        VisualPins.SetValue<uint>(IdHmiBoolAreaSize, UBoolAreaSize);
        VisualPins.SetValue<uint>(IdHmiControlBaseAddr, UControlBaseAddr);
        VisualPins.SetValue<uint>(IdHmiIp1, UControllerIp1);
        VisualPins.SetValue<uint>(IdHmiIp2, UControllerIp2);
        VisualPins.SetValue<uint>(IdHmiIp3, UControllerIp3);
        VisualPins.SetValue<uint>(IdHmiIp4, UControllerIp4);
        VisualPins.SetValue<uint>(IdHmiPort, ControllerTcpPort);
    }

    private Dictionary<int, string> ReadPinGroup(int firstId, int quantity)
    {
        if (quantity <= 0)
            return new Dictionary<int, string>();

        var pinGroup = new Dictionary<int, string>(quantity);

        for (var pinId = firstId; pinId < firstId + quantity; pinId++)
        {
            if (GetPinQuality(pinId) == OpcQuality.Good)
            {
                var pinValue = GetPinValue<string>(pinId);
                if (!string.IsNullOrWhiteSpace(pinValue))
                {
                    pinGroup[pinId - firstId] = pinValue;
                }
            }
        }

        return pinGroup;
    }

    private bool AreFloatsEqual(float a, float b)
    {
        return Math.Abs(a - b) < 0.01;
    }
}