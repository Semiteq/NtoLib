using System.Linq;

using FluentAssertions;

using FluentResults;

using NtoLib.OpcTreeManager.Facade;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class ErrorFlattenTests
{
	[Fact]
	public void Flatten_CausedByChain_YieldsEveryMessageOnceInOrder()
	{
		var root = new Error("snapshot not loaded")
			.CausedBy(new Error("file missing").CausedBy(new Error("C:/tree.json")));

		var messages = OpcTreeManagerService.Flatten(new[] { root }).Select(e => e.Message).ToList();

		messages.Should().Equal("snapshot not loaded", "file missing", "C:/tree.json");
	}

	[Fact]
	public void Flatten_SeveralRootsEachWithACause_YieldsEveryMessageInOrder()
	{
		var first = new Error("scan failed").CausedBy(new Error("config unreadable"));
		var second = new Error("capture failed").CausedBy(new Error("path denied"));

		var messages = OpcTreeManagerService.Flatten(new[] { first, second }).Select(e => e.Message).ToList();

		messages.Should().Equal("scan failed", "config unreadable", "capture failed", "path denied");
	}
}
