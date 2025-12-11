using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using FB;

using InSAT.Library.Interop;

using Microsoft.Extensions.Logging;

using NtoLib.TrendPensManager.Entities;
using NtoLib.TrendPensManager.Facade;
using NtoLib.TrendPensManager.Logging;
using NtoLib.TrendPensManager.Services;

using Serilog;
using Serilog.Extensions.Logging;

namespace NtoLib.TrendPensManager;

[Serializable]
[ComVisible(true)]
[Guid("C92B2C17-3FCD-4D1A-950D-AC25DE65D8D4")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Менеджер перьев окна трендов")]
public class TrendPensManagerFB : StaticFBBase
{
	private const int ExecutePinId = 1;

	private const int ExecutingPinId = 10;
	private const int SuccessPinId = 11;
	private const int LogsPinId = 12;
	private const int ErrorsPinId = 13;

	private static readonly TimeSpan _successFlagDuration = TimeSpan.FromSeconds(1);

	private bool _previousExecuteCommand;
	private DateTime _successFlagResetTimeUtc;
	private bool _isRuntimeInitialized;

	[DisplayName("01. Путь в дереве к корню трендов")]
	public string TrendRootPath { get; set; } = "PLZ_Inject.Графики";

	[DisplayName("02. Путь в дереве к загрузчику конфигурации")]
	public string ConfigLoaderRootPath { get; set; } = "PLZ_Inject.Config.Загрузчик конфигурации";

	[DisplayName("03. Путь в дереве к корню сервисов (источник данных)")]
	public string DataRootPath { get; set; } = "PLZ_Inject.Графики";

	[DisplayName("04. Добавлять БКТ")]
	public bool AddHeaters { get; set; } = true;

	[DisplayName("05. Добавлять БП")]
	public bool AddChamberHeaters { get; set; } = true;

	[DisplayName("06. Добавлять БУЗ")]
	public bool AddShutters { get; set; } = true;

	[DisplayName("07. Добавлять Вакуумметры")]
	public bool AddVacuumMeters { get; set; } = true;

	[DisplayName("08. Добавлять Пирометр")]
	public bool AddPyrometer { get; set; } = true;

	[DisplayName("09. Добавлять Турбины")]
	public bool AddTurbines { get; set; } = true;

	[DisplayName("10. Добавлять Крио")]
	public bool AddCryo { get; set; } = true;

	[DisplayName("11. Добавлять Ионные")]
	public bool AddIon { get; set; } = true;

	[DisplayName("12. Добавлять Интерферометр")]
	public bool AddInterferometer { get; set; } = true;

	[DisplayName("13. Добавлять Газовые линии")]
	public bool AddGases { get; set; } = true;

	[NonSerialized] private ITrendPensService? _trendPensService;
	[NonSerialized] private SerilogLoggerFactory? _loggerFactory;
	[NonSerialized] private StringBuilder? _logBuffer;
	[NonSerialized] private StringBuilderSink? _logSink;

	protected override void ToRuntime()
	{
		base.ToRuntime();
		InitializeRuntime();
	}

	protected override void ToDesign()
	{
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

		UpdateSuccessFlagTimeout();
		ProcessExecuteCommand();
	}

	private void InitializeRuntime()
	{
		if (_isRuntimeInitialized)
		{
			return;
		}

		try
		{
			if (TreeItemHlp?.Project == null)
			{
				throw new InvalidOperationException("TreeItemHlp.Project is null.");
			}

			_logBuffer = new StringBuilder();
			_logSink = new StringBuilderSink(_logBuffer);

			var serilogLogger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Sink(_logSink)
				.CreateLogger();

			_loggerFactory = new SerilogLoggerFactory(serilogLogger);

			var serviceFilter = new ServiceFilter();
			var treeTraversal = new TreeTraversalService(TreeItemHlp.Project, serviceFilter, _loggerFactory);
			var configLoaderReader = new ConfigLoaderReader(TreeItemHlp.Project, _loggerFactory);
			var penSequenceBuilder = new PenSequenceBuilder(_loggerFactory);

			var trendOperations = new TrendOperations(TreeItemHlp.Project, _loggerFactory.CreateLogger<TrendOperations>());
			var trendPenApplicator = new TrendPenApplicator(TreeItemHlp.Project, trendOperations, _loggerFactory.CreateLogger<TrendPenApplicator>());

			var trendPensLogger = _loggerFactory.CreateLogger<TrendPensService>();

			_trendPensService = new TrendPensService(
				TreeItemHlp.Project,
				treeTraversal,
				configLoaderReader,
				penSequenceBuilder,
				trendPenApplicator,
				trendPensLogger);

			_isRuntimeInitialized = true;
		}
		catch (Exception ex)
		{
			_isRuntimeInitialized = false;
			SetPinValue(ErrorsPinId, $"Initialization failed: {ex.Message}");
		}
	}

	private void CleanupRuntime()
	{
		_isRuntimeInitialized = false;
		_trendPensService = null;

		_loggerFactory?.Dispose();
		_loggerFactory = null;
		_logBuffer = null;
		_logSink = null;
	}

	private void ProcessExecuteCommand()
	{
		if (_trendPensService == null)
		{
			return;
		}

		var currentExecuteCommand = GetPinValue<bool>(ExecutePinId);
		var isRisingEdge = currentExecuteCommand && !_previousExecuteCommand;
		_previousExecuteCommand = currentExecuteCommand;

		if (!isRisingEdge)
		{
			return;
		}

		ExecuteAutoConfig();
	}

	private void ExecuteAutoConfig()
	{
		_logSink?.Clear();
		SetPinValue(LogsPinId, string.Empty);
		SetPinValue(ErrorsPinId, string.Empty);
		SetPinValue(SuccessPinId, false);
		SetPinValue(ExecutingPinId, true);

		var options = CreateServiceSelectionOptions();
		var result = _trendPensService!.AutoConfigurePens(
			TrendRootPath,
			ConfigLoaderRootPath,
			DataRootPath,
			options);

		FlushLogsToPin();

		SetPinValue(ExecutingPinId, false);

		if (result.IsFailed)
		{
			SetPinValue(SuccessPinId, false);
			SetPinValue(ErrorsPinId, string.Join("|", result.Errors));
			return;
		}

		SetPinValue(SuccessPinId, true);
		_successFlagResetTimeUtc = DateTime.UtcNow.Add(_successFlagDuration);
	}

	private ServiceSelectionOptions CreateServiceSelectionOptions()
	{
		return new ServiceSelectionOptions
		{
			AddHeaters = AddHeaters,
			AddChamberHeaters = AddChamberHeaters,
			AddShutters = AddShutters,
			AddVacuumMeters = AddVacuumMeters,
			AddPyrometer = AddPyrometer,
			AddTurbines = AddTurbines,
			AddCryo = AddCryo,
			AddIon = AddIon,
			AddInterferometer = AddInterferometer,
			AddGases = AddGases
		};
	}

	private void FlushLogsToPin()
	{
		var logs = _logSink?.GetLogs() ?? string.Empty;
		SetPinValue(LogsPinId, logs);
	}

	private void UpdateSuccessFlagTimeout()
	{
		var isSuccess = GetPinValue<bool>(SuccessPinId);

		if (!isSuccess)
		{
			return;
		}

		if (DateTime.UtcNow >= _successFlagResetTimeUtc)
		{
			SetPinValue(SuccessPinId, false);
		}
	}
}
