using System;
using System.IO;
using System.Net;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

public sealed class FbRuntimeOptionsProvider : IRuntimeOptionsProvider
{
	private readonly MbeTableFB _fb;

	public FbRuntimeOptionsProvider(MbeTableFB fb)
	{
		_fb = fb ?? throw new ArgumentNullException(nameof(fb));
	}

	public RuntimeOptions GetCurrent()
	{
		var ip = new IPAddress(new[]
		{
			(byte)_fb.UControllerIp1,
			(byte)_fb.UControllerIp2,
			(byte)_fb.UControllerIp3,
			(byte)_fb.UControllerIp4
		});

		var verifyDelayMs = 200;
		var wordOrder = _fb.WordOrder;

		var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var defaultLogsDir = Path.Combine(appData, "NtoLibLogs");
		var dirExpanded = Environment.ExpandEnvironmentVariables(_fb.LogDirPath ?? string.Empty);
		var effectiveDir = string.IsNullOrWhiteSpace(dirExpanded) ? defaultLogsDir : dirExpanded;
		var logFilePath = Path.Combine(effectiveDir, "mbe-table.log");

		return new RuntimeOptions(
			IpAddress: ip,
			Port: (int)_fb.ControllerTcpPort,
			UnitId: (byte)_fb.UnitId,
			TimeoutMs: (int)_fb.TimeoutMs,
			MaxRetries: (int)_fb.MaxRetries,
			BackoffDelayMs: (int)_fb.BackoffDelayMs,
			MagicNumber: (int)_fb.MagicNumber,
			VerifyDelayMs: verifyDelayMs,
			ControlRegister: (int)_fb.UControlBaseAddr,
			FloatBaseAddr: (int)_fb.UFloatBaseAddr,
			FloatAreaSize: (int)_fb.UFloatAreaSize,
			IntBaseAddr: (int)_fb.UIntBaseAddr,
			IntAreaSize: (int)_fb.UIntAreaSize,
			WordOrder: wordOrder,
			Epsilon: _fb.Epsilon,
			LogToFile: _fb.LogToFile,
			LogFilePath: logFilePath
		);
	}
}
