using System;
using System.IO;
using System.Net;

using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTableEditor;

using Xunit;

namespace Tests.MbeTable.Infrastructure;

public sealed class EditorRuntimeOptionsProviderTests
{
	[Fact]
	public void GetCurrent_ExposesLoggingAndEpsilon()
	{
		var provider = new EditorRuntimeOptionsProvider(logToFile: true, logDirPath: @"C:\Temp\Logs", epsilon: 0.25f);

		var options = provider.GetCurrent();

		options.LogToFile.Should().BeTrue();
		options.Epsilon.Should().Be(0.25f);
		options.LogFilePath.Should().Be(@"C:\Temp\Logs\mbe-table.log");
	}

	[Fact]
	public void GetCurrent_LeavesModbusFieldsAtDefaults()
	{
		var provider = new EditorRuntimeOptionsProvider(logToFile: false, logDirPath: string.Empty, epsilon: 0.1f);

		var options = provider.GetCurrent();

		options.IpAddress.Should().Be(IPAddress.None);
		options.Port.Should().Be(0);
		options.UnitId.Should().Be(0);
		options.TimeoutMs.Should().Be(0);
		options.MaxRetries.Should().Be(0);
		options.BackoffDelayMs.Should().Be(0);
		options.MagicNumber.Should().Be(0);
		options.VerifyDelayMs.Should().Be(0);
		options.ControlRegister.Should().Be(0);
		options.FloatBaseAddr.Should().Be(0);
		options.FloatAreaSize.Should().Be(0);
		options.IntBaseAddr.Should().Be(0);
		options.IntAreaSize.Should().Be(0);
		options.WordOrder.Should().Be(WordOrder.HighLow);
	}

	[Fact]
	public void GetCurrent_FallsBackToDefaultLogDir_WhenPathBlank()
	{
		var provider = new EditorRuntimeOptionsProvider(logToFile: true, logDirPath: "   ", epsilon: 0.0f);

		var options = provider.GetCurrent();

		options.LogFilePath.Should().EndWith(@"NtoLibLogs\mbe-table.log");
	}

	[Fact]
	public void GetCurrent_FallsBackToDefaultLogDir_WhenPathNull()
	{
		var provider = new EditorRuntimeOptionsProvider(logToFile: true, logDirPath: null!, epsilon: 0.0f);

		var options = provider.GetCurrent();

		options.LogFilePath.Should().EndWith(@"NtoLibLogs\mbe-table.log");
	}

	[Fact]
	public void GetCurrent_ExpandsEnvironmentVariablesInLogDir()
	{
		const string variableName = "NTOLIB_EDITOR_LOG_TEST_DIR";
		var expected = Path.Combine(Path.GetTempPath(), "NtoLibEditorLogTest");
		Environment.SetEnvironmentVariable(variableName, expected);

		try
		{
			var provider = new EditorRuntimeOptionsProvider(
				logToFile: true,
				logDirPath: $"%{variableName}%",
				epsilon: 0.0f);

			var options = provider.GetCurrent();

			options.LogFilePath.Should().Be(Path.Combine(expected, "mbe-table.log"));
		}
		finally
		{
			Environment.SetEnvironmentVariable(variableName, null);
		}
	}
}
