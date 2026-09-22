using System;
using System.Collections.Generic;
using System.IO;

using FluentAssertions;

using NtoLib.OpcTreeManager.Config;
using NtoLib.OpcTreeManager.Entities;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

/// <summary>
/// The writer is the only producer of tree.json, so it refuses an empty snapshot rather than
/// serialising it over the reference file.
/// </summary>
public sealed class TreeSnapshotWriterTests
{
	[Fact]
	public void Write_EmptySnapshot_LeavesExistingFileUntouched()
	{
		const string Reference = "{\n  \"NodeA\": { \"links\": [], \"scadaItem\": null }\n}";
		var path = TempPath();
		File.WriteAllText(path, Reference);

		try
		{
			var result = TreeSnapshotWriter.Write(EmptySnapshot(), path);

			result.IsFailed.Should().BeTrue();
			File.ReadAllText(path).Should().Be(Reference);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void Write_EmptySnapshot_CreatesNoFile()
	{
		var path = TempPath();

		var result = TreeSnapshotWriter.Write(EmptySnapshot(), path);

		result.IsFailed.Should().BeTrue();
		File.Exists(path).Should().BeFalse();
	}

	[Fact]
	public void Write_NonEmptySnapshot_RoundTripsThroughTheLoader()
	{
		var path = TempPath();
		var snapshot = new Dictionary<string, NodeSnapshot>(StringComparer.Ordinal)
		{
			["Valves"] = new NodeSnapshot
			{
				ScadaItem = new OpcScadaItemDto
				{
					Name = "Valves",
					PinValueType = "0",
					DeadbandType = "None",
					Items = new List<OpcScadaItemDto>
					{
						new OpcScadaItemDto
						{
							Name = "VPG1",
							PinValueType = "0",
							DeadbandType = "None",
							Items = new List<OpcScadaItemDto>(),
						},
					},
				},
				Links = new[]
				{
					new LinkEntry
					{
						LocalPinPath = "Project.OpcUaFB.MBE.Valves.VPG1",
						ExternalPinPath = "Project.Valves.VPG1.State",
						LinkType = LinkTypes.DirectPin,
					},
				},
			},
		};

		try
		{
			TreeSnapshotWriter.Write(snapshot, path).IsSuccess.Should().BeTrue();

			var reloaded = TreeSnapshotLoader.Load(path);

			reloaded.IsSuccess.Should().BeTrue();
			reloaded.Value.Should().BeEquivalentTo(snapshot, "the writer and the loader share one format");
		}
		finally
		{
			File.Delete(path);
		}
	}

	private static Dictionary<string, NodeSnapshot> EmptySnapshot()
	{
		return new Dictionary<string, NodeSnapshot>(StringComparer.Ordinal);
	}

	private static string TempPath()
	{
		return Path.Combine(Path.GetTempPath(), $"treesnapshot-write-{Guid.NewGuid():N}.json");
	}
}
