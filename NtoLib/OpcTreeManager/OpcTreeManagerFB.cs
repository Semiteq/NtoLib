using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;

using InSAT.Library.Interop;

using NtoLib.OpcTreeManager.Facade;
using NtoLib.OpcTreeManager.Logging;

using Serilog;
using Serilog.Core;

namespace NtoLib.OpcTreeManager;

[Serializable]
[ComVisible(true)]
[Guid("3F7A9C2E-B841-4D56-A0F3-8C1E2D5B7094")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Менеджер дерева OPC")]
public sealed class OpcTreeManagerFB : StaticFBBase
{
	// Input pins use IDs 1..99; output pins use IDs 100+.
	private const int ExecutePinId = 1;
	private const int CancelPinId = 2;
	private const int ExecuteSnapshotPinId = 3;

	private const int IsPendingPinId = 101;
	private const int FailedPinId = 102;

	[NonSerialized] private bool _isRuntimeInitialized;
	[NonSerialized] private bool _previousExecute;
	[NonSerialized] private bool _previousCancel;
	[NonSerialized] private bool _previousExecuteSnapshot;
	[NonSerialized] private Logger? _logger;
	[NonSerialized] private ILogger? _log;
	[NonSerialized] private IOpcTreeManagerService? _service;

	[DisplayName("Целевой проект")]
	[Category("Настройки операции")]
	[Description("Имя целевого проекта из config.yaml. Используется для определения узлов, которые нужно сохранить.")]
	public string TargetProject { get; set; } = "MBE";

	[DisplayName("Путь к OPC FB")]
	[Category("Настройки операции")]
	[Description("Полный путь к узлу OPC UA FB в дереве проекта (например, Система.АРМ.OPC UA Siemens).")]
	public string OpcFbPath { get; set; } = "Система.АРМ.OPC UA Siemens";

	[DisplayName("Имя группы OPC")]
	[Category("Настройки операции")]
	[Description("Имя группы в ScadaRootNode, прямые потомки которой управляются ФБ.")]
	public string GroupName { get; set; } = "MBE";

	[DisplayName("Путь к tree.json")]
	[Category("Настройки файлов")]
	[Description("Полный путь к файлу эталонного снапшота дерева OPC (используется в режиме Expand и при записи снапшота).")]
	public string TreeJsonPath { get; set; } = @"C:\DISTR\Config\OpcTreeManager\tree.json";

	[DisplayName("Путь к config.yaml")]
	[Category("Настройки файлов")]
	[Description("Полный путь к файлу конфигурации с маппингом узлов OPC на проекты.")]
	public string ConfigYamlPath { get; set; } = @"C:\DISTR\Config\OpcTreeManager\config.yaml";

	[DisplayName("Путь для записи лога")]
	[Category("Настройки логирования")]
	[Description("Полный путь к файлу лога работы функционального блока.")]
	public string LogFilePath { get; set; } = @"C:\DISTR\Logs\OpcTreeManager\opc-tree-manager.log";

	protected override void ToRuntime()
	{
		base.ToRuntime();

		if (TreeItemHlp?.Project == null)
		{
			throw new InvalidOperationException("TreeItemHlp.Project is null.");
		}

		InitializeRuntime();
	}

	protected override void ToDesign()
	{
		FlushPendingPlan();
		base.ToDesign();
		CleanupRuntime();
	}

	protected override void UpdateData()
	{
		base.UpdateData();

		if (!_isRuntimeInitialized)
		{
			return;
		}

		ProcessExecuteCommand();
		ProcessCancelCommand();
		ProcessSnapshotCommand();
	}

	private void InitializeRuntime()
	{
		if (_isRuntimeInitialized)
		{
			return;
		}

		try
		{
			_logger = OpcLoggerFactory.Build(LogFilePath);
			_log = _logger.ForContext<OpcTreeManagerFB>();
			_service = new OpcTreeManagerService(TreeItemHlp!.Project, _logger);
			_previousExecute = false;
			_previousCancel = false;
			_previousExecuteSnapshot = false;
			_isRuntimeInitialized = true;

			SetPinValue(IsPendingPinId, false, DateTime.UtcNow);
			SetPinValue(FailedPinId, false, DateTime.UtcNow);
			_log.Information(
				"Initialized; OpcFbPath={OpcFbPath} GroupName={GroupName} TargetProject={TargetProject}",
				OpcFbPath, GroupName, TargetProject);
		}
		catch (Exception ex)
		{
			_isRuntimeInitialized = false;
			_log?.Error(ex, "Initialization failed");
			_logger?.Dispose();
			_logger = null;
			_log = null;
			SetPinValue(IsPendingPinId, false, DateTime.UtcNow);
			SetPinValue(FailedPinId, true, DateTime.UtcNow);
		}
	}

	private void CleanupRuntime()
	{
		_isRuntimeInitialized = false;
		_service = null;
		_logger?.Dispose();
		_logger = null;
		_log = null;
	}

	private bool ConsumeRisingEdge(int pinId, ref bool previousState)
	{
		var current = GetPinValue<bool>(pinId);
		var isRising = current && !previousState;
		previousState = current;
		return isRising;
	}

	private void ProcessExecuteCommand()
	{
		if (!ConsumeRisingEdge(ExecutePinId, ref _previousExecute))
		{
			return;
		}

		if (_service == null)
		{
			return;
		}

		SetPinValue(FailedPinId, false, DateTime.UtcNow);

		var result = _service.ScanAndValidate(TargetProject, OpcFbPath, GroupName, TreeJsonPath, ConfigYamlPath);

		if (result.IsFailed)
		{
			_log?.Error("ScanAndValidate failed: {Errors}", string.Join("; ", result.Errors));
			SetPinValue(FailedPinId, true, DateTime.UtcNow);
		}

		SetPinValue(IsPendingPinId, _service.HasPendingTask, DateTime.UtcNow);
	}

	private void ProcessCancelCommand()
	{
		if (!ConsumeRisingEdge(CancelPinId, ref _previousCancel))
		{
			return;
		}

		if (_service == null)
		{
			return;
		}

		_service.Cancel();
		SetPinValue(IsPendingPinId, false, DateTime.UtcNow);
	}

	private void ProcessSnapshotCommand()
	{
		if (!ConsumeRisingEdge(ExecuteSnapshotPinId, ref _previousExecuteSnapshot))
		{
			return;
		}

		if (_service == null)
		{
			return;
		}

		SetPinValue(FailedPinId, false, DateTime.UtcNow);

		var result = _service.CaptureAndWriteSnapshot(OpcFbPath, GroupName, TreeJsonPath);

		if (result.IsFailed)
		{
			_log?.Error("CaptureAndWriteSnapshot failed: {Errors}", string.Join("; ", result.Errors));
			SetPinValue(FailedPinId, true, DateTime.UtcNow);
		}
	}

	private void FlushPendingPlan()
	{
		if (_service == null || !_service.HasPendingTask)
		{
			return;
		}

		// Transfer logger ownership to the deferred executor before posting.
		// ExecuteDeferred captures 'logger' via DeferredExecutor closures, which dispose it
		// after deferred execution completes. Setting _logger = null here prevents
		// CleanupRuntime from double-disposing it.
		var logger = _logger;
		_logger = null;
		_log = null;
		_service.ExecuteDeferred(logger);
	}
}
