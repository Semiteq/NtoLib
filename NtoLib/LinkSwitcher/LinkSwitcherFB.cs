using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using FB;

using InSAT.Library.Interop;

using MasterSCADA.Hlp;

using NtoLib.LinkSwitcher.Entities;
using NtoLib.LinkSwitcher.Facade;

using Serilog;
using Serilog.Core;

namespace NtoLib.LinkSwitcher;

[Serializable]
[ComVisible(true)]
[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Переключатель связей")]
public sealed class LinkSwitcherFB : StaticFBBase
{
	private const int ExecutePinId = 1;
	private const int ForwardPinId = 2;
	private const int DryRunPinId = 3;
	private const int CancelPinId = 4;

	private const int SuccessPinId = 101;
	private const int IsPendingPinId = 104;

	private const int MaxDeferredRetries = 100;
	private const int DeferredRetryIntervalMs = 200;

	[NonSerialized] private ILinkSwitcherService? _service;
	[NonSerialized] private Logger? _serilogLogger;
	[NonSerialized] private bool _previousExecute;
	[NonSerialized] private bool _previousCancel;
	[NonSerialized] private bool _isRuntimeInitialized;

	[DisplayName("Путь для поиска пар объектов")]
	[Category("Настройки поиска")]
	[Description("Укажите путь в дереве проекта, где находятся пары объектов для перелинковки. Например: 'Компьютер1/Контроллер1'.")]
	public string SearchPath { get; set; } = "Система.АРМ.OPC UA Siemens.ServerInterfaces.MBE";

	[DisplayName("Путь для записи лога")]
	[Category("Настройки логирования")]
	[Description("Укажите полный путь к файлу для записи лога работы функции.")]
	public string LogFilePath { get; set; } = @"C:\DISTR\Logs\link-switcher.log";

	protected override void ToRuntime()
	{
		base.ToRuntime();
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

			_serilogLogger = BuildLogger(LogFilePath);
			_service = new LinkSwitcherService(TreeItemHlp.Project, _serilogLogger);
			_previousExecute = false;
			_previousCancel = false;
			_isRuntimeInitialized = true;

			_serilogLogger.Information("LinkSwitcherFB initialized");
		}
		catch (Exception exception)
		{
			_isRuntimeInitialized = false;
			_serilogLogger?.Error(exception, "LinkSwitcherFB initialization failed");
			WriteOutputs(false, false);
		}
	}

	private void CleanupRuntime()
	{
		_isRuntimeInitialized = false;
		_service = null;
		_serilogLogger = null;
	}

	private void ProcessExecuteCommand()
	{
		var currentExecute = GetPinValue<bool>(ExecutePinId);
		var isRisingEdge = currentExecute && !_previousExecute;
		_previousExecute = currentExecute;

		if (!isRisingEdge || _service == null)
		{
			return;
		}

		var forward = GetPinValue<bool>(ForwardPinId);
		var isDryRun = GetPinValue<bool>(DryRunPinId);

		var scanResult = _service.ScanAndValidate(SearchPath, forward);
		if (scanResult.IsFailed)
		{
			WriteOutputs(false, false);
			return;
		}

		var plan = scanResult.Value;

		if (isDryRun)
		{
			_service.DryRun(plan);
			WriteOutputs(true, false);
			return;
		}

		WriteOutputs(false, true);
	}

	private void ProcessCancelCommand()
	{
		var currentCancel = GetPinValue<bool>(CancelPinId);
		var isRisingEdge = currentCancel && !_previousCancel;
		_previousCancel = currentCancel;

		if (!isRisingEdge || _service == null)
		{
			return;
		}

		_service.Cancel();
		WriteOutputs(false, false);
	}

	private void FlushPendingPlan()
	{
		if (_service == null)
		{
			_serilogLogger?.Dispose();
			return;
		}

		if (!_service.HasPendingTask)
		{
			_serilogLogger?.Dispose();
			return;
		}

		var plan = _service.PendingPlan;
		if (plan == null)
		{
			_serilogLogger?.Dispose();
			return;
		}

		var service = _service;
		var logger = _serilogLogger;
		var project = TreeItemHlp?.Project;

		if (project == null)
		{
			logger?.Error("Cannot flush pending plan: IProjectHlp is null");
			logger?.Dispose();
			return;
		}

		PostDeferredExecution(service, plan, logger, project, retriesRemaining: MaxDeferredRetries);
	}

	private static void PostDeferredExecution(
		ILinkSwitcherService service,
		SwitchPlan plan,
		Logger? logger,
		IProjectHlp project,
		int retriesRemaining)
	{
		var timer = new Timer { Interval = DeferredRetryIntervalMs };
		var retries = retriesRemaining;

		timer.Tick += (_, _) =>
		{
			if (project.InRuntime)
			{
				retries--;

				if (retries <= 0)
				{
					timer.Stop();
					timer.Dispose();
					logger?.Error(
						"Deferred execution aborted: IProjectHlp.InRuntime is still true after {MaxRetries} retries ({TotalSeconds}s)",
						MaxDeferredRetries,
						MaxDeferredRetries * DeferredRetryIntervalMs / 1000.0);
					logger?.Dispose();
				}

				return;
			}

			timer.Stop();
			timer.Dispose();

			try
			{
				var result = service.Execute(plan);

				if (result.IsSuccess)
				{
					logger?.Information("Deferred execution completed successfully");
				}
				else
				{
					var errorMessage = string.Join("; ", result.Errors);
					logger?.Error("Deferred execution completed with errors: {ErrorMessage}", errorMessage);
				}
			}
			catch (Exception exception)
			{
				logger?.Error(exception, "Deferred execution failed with exception");
			}
			finally
			{
				logger?.Dispose();
			}
		};

		timer.Start();
	}

	private void WriteOutputs(bool success, bool isPending)
	{
		var now = DateTime.UtcNow;

		SetPinValue(SuccessPinId, success, now);
		SetPinValue(IsPendingPinId, isPending, now);
	}

	private static Logger BuildLogger(string logFilePath)
	{
		const string Template = "{Timestamp:O} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
		var invariant = CultureInfo.InvariantCulture;

		TryEnsureDirectory(logFilePath);

		return new LoggerConfiguration()
			.MinimumLevel.Information()
			.WriteTo.Debug(outputTemplate: Template, formatProvider: invariant)
			.WriteTo.File(
				path: logFilePath,
				rollingInterval: RollingInterval.Infinite,
				fileSizeLimitBytes: 5 * 1024 * 1024,
				rollOnFileSizeLimit: true,
				retainedFileCountLimit: 5,
				shared: true,
				outputTemplate: Template,
				formatProvider: invariant)
			.CreateLogger();
	}

	private static void TryEnsureDirectory(string filePath)
	{
		try
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
		catch
		{
			// Best-effort directory creation; logging may fail if this fails
		}
	}
}
