#nullable enable
namespace NtoLib.Recipes.MbeTable;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

[CatID(CatIDs.CATID_OTHER)]
[Guid("DFB05172-07CD-492C-925E-A091B197D8A8")]
[FBOptions(FBOptions.EnableChangeConfigInRT)]
[VisualControls(typeof(TableControl))]
[DisplayName("Таблица рецептов MBE")]
[ComVisible(true)]
[Serializable]
public class MbeTableFB : VisualFBBase
{
    public IServiceProvider? ServiceProvider => _serviceProvider;

    [NonSerialized] private IServiceProvider? _serviceProvider;
    [NonSerialized] private TimerService _timerService = null!;
    [NonSerialized] private IPlcStateMonitor _plcStateMonitor = null!;
    [NonSerialized] private IActionTargetProvider _actionTargetProvider = null!;
    [NonSerialized] private IPlcRecipeStatusProvider _plcRecipeStatusProvider = null!;

    // Runtime snapshot of pin groups loaded from PinGroups.json: GroupName -> (FirstPinId, PinQuantity)
    [NonSerialized] private Dictionary<string, (int FirstPinId, int PinQuantity)> _pinGroups =
        new Dictionary<string, (int FirstPinId, int PinQuantity)>(StringComparer.OrdinalIgnoreCase);

    #region Numerical Properties

    private const int IdRecipeActive = 1;
    private const int IdCurrentLine = 3;
    private const int IdStepCurrentTime = 4;
    private const int IdForLoopCount1 = 5;
    private const int IdForLoopCount2 = 6;
    private const int IdForLoopCount3 = 7;
    private const int IdEnaSend = 8;
    private const int IdTotalTimeLeft = 101;
    private const int IdLineTimeLeft = 102;

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

    [DisplayName(" 5.  Базовый адрес контрольной области")]
    [Description("Определяет начальный адрес, где располагается зона контрольных данных (3 слова)")]
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

    public IReadOnlyCollection<string> GetDefinedGroupNames() => _pinGroups.Keys.ToArray();

    public Dictionary<int, string> ReadTargets(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentNullException(nameof(groupName));

        if (!_pinGroups.TryGetValue(groupName, out var cfg))
            throw new InvalidOperationException($"Group '{groupName}' is not defined in PinGroups.json.");

        return ReadPinGroup(cfg.FirstPinId, cfg.PinQuantity, groupName);
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        if (_plcStateMonitor == null || _plcRecipeStatusProvider == null || _timerService == null)
            return;

        var stepCurrentTime = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<float>(IdStepCurrentTime) : -1f;
        var lineNumber = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdCurrentLine) : -1;
        var forLoopCount1 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount1) : -1;
        var forLoopCount2 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount2) : -1;
        var forLoopCount3 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount3) : -1;

        _plcStateMonitor.UpdateState(lineNumber, forLoopCount1, forLoopCount2, forLoopCount3, stepCurrentTime);

        var isRecipeActive = GetPinQuality(IdRecipeActive) is OpcQuality.Good && GetPinValue<bool>(IdRecipeActive);
        var currentLine = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdCurrentLine) : -1;
        var isEnaSend = GetPinQuality(IdEnaSend) is OpcQuality.Good && GetPinValue<bool>(IdEnaSend);

        _plcRecipeStatusProvider.UpdateStatus(isRecipeActive, isEnaSend, currentLine);

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

    /// <summary>
    /// Creates pin groups and pins dynamically based on PinGroups.json via PinMapInitializer.
    /// Called by SCADA before constructors; configuration must be read right here.
    /// </summary>
    protected override void CreatePinMap(bool newObject)
    {
        base.CreatePinMap(newObject);

        var initializer = new PinMapInitializer();
        _pinGroups = initializer.InitializePinsFromConfig(this);

        FirePinSpaceChanged();
        Debug.Print("Pins were created from PinGroups.json (via PinMapInitializer).");
    }

    private void InitializeServices()
    {
        if (_serviceProvider != null) return;

        _serviceProvider = MbeTableServiceConfigurator.ConfigureServices(this);

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

    private void CleanupServices()
    {
        if (_serviceProvider == null) return;

        var debugLogger = _serviceProvider.GetService<ILogger>();
        debugLogger?.Log("MbeTableFB: Entering Design mode or Disposing. Cleaning up services.");

        if (_timerService != null)
            _timerService.TimesUpdated -= OnTimesUpdated;

        if (_serviceProvider is IDisposable disposableProvider)
            disposableProvider.Dispose();

        _plcStateMonitor = null!;
        _actionTargetProvider = null!;
        _plcRecipeStatusProvider = null!;
        _timerService = null!;
        _serviceProvider = null!;
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

    private Dictionary<int, string> ReadPinGroup(int firstId, int quantity, string groupNameForDefault)
    {
        var pinGroup = new Dictionary<int, string>(quantity);

        for (var offset = 0; offset < quantity; offset++)
        {
            var pinId = firstId + offset;
            string value;

            if (GetPinQuality(pinId) == OpcQuality.Good)
            {
                var pinValue = GetPinValue<string>(pinId);
                value = !string.IsNullOrWhiteSpace(pinValue) ? pinValue : $"{groupNameForDefault}{offset}";
            }
            else
            {
                value = $"{groupNameForDefault}{offset}";
            }

            pinGroup[offset] = value;
        }

        return pinGroup;
    }

    private static bool AreFloatsEqual(float a, float b) => Math.Abs(a - b) < 0.01f;
}