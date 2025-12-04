using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using FB;

using InSAT.Library.Interop;

using Microsoft.Extensions.Logging;

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
	private const int DumpTreePinId = 2;

	private const int DonePinId = 10;
	private const int LogsPinId = 11;
	private const int ErrorsPinId = 12;

	private static readonly TimeSpan _doneFlagDuration = TimeSpan.FromSeconds(1);

	private bool _previousExecuteCommand;
	private bool _previousDumpTreeCommand;
	private DateTime _doneFlagResetTimeUtc;
	private bool _isRuntimeInitialized;

	[DisplayName("01. Путь в дереве к корню трендов")]
	public string TrendRootPath { get; set; } = "PLZ_Inject.Графики";

	[DisplayName("02. Путь в дереве к загрузчику конфигурации")]
	public string ConfigLoaderRootPath { get; set; } = "PLZ_Inject.Config.Загрузчик конфигурации";

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

		UpdateDoneFlagTimeout();
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

			var trendPensLogger = _loggerFactory.CreateLogger<TrendPensService>();

			var treeTraversal = new TreeTraversalService(TreeItemHlp.Project, _loggerFactory);
			var configLoaderReader = new ConfigLoaderReader(TreeItemHlp.Project, _loggerFactory);
			var penSequenceBuilder = new PenSequenceBuilder(_loggerFactory);

			_trendPensService = new TrendPensService(
				TreeItemHlp.Project,
				treeTraversal,
				configLoaderReader,
				penSequenceBuilder,
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

		var result = _trendPensService!.AutoConfigurePens(TrendRootPath, ConfigLoaderRootPath);

		FlushLogsToPin();

		if (result.IsFailed)
		{
			SetPinValue(DonePinId, false);
			SetPinValue(ErrorsPinId, string.Join("|", result.Errors));
			return;
		}

		var autoConfigResult = result.Value;

		SetPinValue(DonePinId, true);
		_doneFlagResetTimeUtc = DateTime.UtcNow.Add(_doneFlagDuration);

		if (autoConfigResult.Warnings.Count > 0)
		{
			SetPinValue(ErrorsPinId, string.Join("|", autoConfigResult.Warnings));
		}
	}

	private void FlushLogsToPin()
	{
		var logs = _logSink?.GetLogs() ?? string.Empty;
		SetPinValue(LogsPinId, logs);
	}

	private void UpdateDoneFlagTimeout()
	{
		var isDone = GetPinValue<bool>(DonePinId);

		if (!isDone)
		{
			return;
		}

		if (DateTime.UtcNow >= _doneFlagResetTimeUtc)
		{
			SetPinValue(DonePinId, false);
		}
	}
}
