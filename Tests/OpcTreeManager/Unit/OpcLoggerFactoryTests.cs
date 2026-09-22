using System;
using System.IO;

using FluentAssertions;

using NtoLib.OpcTreeManager.Logging;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class OpcLoggerFactoryTests
{
	[Fact]
	public void Build_OutputTemplate_OmitsSourceContext()
	{
		var path = TempPath();

		try
		{
			using (var logger = OpcLoggerFactory.Build(path))
			{
				logger.ForContext<OpcLoggerFactoryTests>().Information("Runtime entered");
			}

			var firstLine = File.ReadAllLines(path)[0];

			firstLine.Should().MatchRegex(@"^\S+ \[INF\] Runtime entered$");
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void Build_UnwritablePath_ReturnsLoggerThatWritesNothing()
	{
		var blocker = TempPath();
		File.WriteAllText(blocker, "not a directory");
		var unwritablePath = Path.Combine(blocker, "sub", "opc-tree-manager.log");

		try
		{
			var logger = OpcLoggerFactory.Build(unwritablePath);

			logger.Should().NotBeNull();

			Action write = () =>
			{
				logger.Information("Runtime entered");
				logger.Dispose();
			};

			write.Should().NotThrow();
			Directory.Exists(blocker).Should().BeFalse();
			File.ReadAllText(blocker).Should().Be("not a directory");
		}
		finally
		{
			File.Delete(blocker);
		}
	}

	private static string TempPath()
	{
		return Path.Combine(Path.GetTempPath(), $"opc-logger-factory-{Guid.NewGuid():N}.log");
	}
}
