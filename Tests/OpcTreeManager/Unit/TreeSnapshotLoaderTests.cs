using System;
using System.IO;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Config;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class TreeSnapshotLoaderTests
{
	[Fact]
	public void Load_BlankPathLinks_ReportsDroppedCount()
	{
		// Two links are valid, two have a blank pin path (one blank local, one blank external).
		using var file = TempJson(@"
{
  ""NodeA"": {
    ""links"": [
      { ""localPin"": ""NodeA.Kp"", ""externalPin"": ""Ctrl.Kp"", ""linkType"": ""DirectPin"" },
      { ""localPin"": """", ""externalPin"": ""Ctrl.Ki"", ""linkType"": ""DirectPin"" }
    ],
    ""scadaItem"": null
  },
  ""NodeB"": {
    ""links"": [
      { ""localPin"": ""NodeB.Setpoint"", ""externalPin"": ""Ctrl.Setpoint"", ""linkType"": ""DirectPin"" },
      { ""localPin"": ""NodeB.Kd"", ""externalPin"": ""   "", ""linkType"": ""DirectPin"" }
    ],
    ""scadaItem"": null
  }
}");

		var result = TreeSnapshotLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();

		var dropped = result.Successes.OfType<DroppedLinksSuccess>().Single();
		dropped.Count.Should().Be(2);

		// The valid links survive; only the blank-path ones are dropped.
		result.Value["NodeA"].Links.Should().ContainSingle(l => l.LocalPinPath == "NodeA.Kp");
		result.Value["NodeB"].Links.Should().ContainSingle(l => l.LocalPinPath == "NodeB.Setpoint");
	}

	[Fact]
	public void Load_CleanSnapshot_ReportsZeroDroppedAndKeepsEveryLink()
	{
		using var file = TempJson(@"
{
  ""NodeA"": {
    ""links"": [
      { ""localPin"": ""NodeA.Kp"", ""externalPin"": ""Ctrl.Kp"", ""linkType"": ""DirectPin"" },
      { ""localPin"": ""NodeA.Ki"", ""externalPin"": ""Ctrl.Ki"", ""linkType"": ""DirectPin"" }
    ],
    ""scadaItem"": null
  }
}");

		var result = TreeSnapshotLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();

		var dropped = result.Successes.OfType<DroppedLinksSuccess>().Single();
		dropped.Count.Should().Be(0);

		result.Value["NodeA"].Links.Should().HaveCount(2);
	}

	private static TempJsonFile TempJson(string content)
	{
		var path = Path.Combine(Path.GetTempPath(), $"treesnapshot-{Guid.NewGuid():N}.json");
		File.WriteAllText(path, content);
		return new TempJsonFile(path);
	}

	private sealed class TempJsonFile : IDisposable
	{
		public string Path { get; }

		public TempJsonFile(string path)
		{
			Path = path;
		}

		public void Dispose()
		{
			try
			{
				if (File.Exists(Path))
				{
					File.Delete(Path);
				}
			}
			catch
			{
				// best-effort cleanup
			}
		}
	}
}
