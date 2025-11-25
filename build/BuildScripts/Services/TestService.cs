using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BuildScripts.Infrastructure;

using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities;

using Serilog;

namespace BuildScripts.Services;

public sealed class TestService
{
	private readonly BuildContext _ctx;

	public TestService(BuildContext ctx)
	{
		_ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
	}

	public void RunTests(string categoryFilter, string componentFilter, bool detailedLog)
	{
		var filter = BuildFilter(categoryFilter, componentFilter);

		DotNetTasks.DotNetTest(s => s
			.SetProjectFile(_ctx.Solution.GetProject("NtoLib.Test"))
			.SetConfiguration(_ctx.Configuration)
			.SetNoBuild(true)
			.SetNoRestore(true)
			.When(!string.IsNullOrWhiteSpace(filter), x => x.SetFilter(filter))
			.When(detailedLog, x => x.SetVerbosity(DotNetVerbosity.detailed)));

		Log.Information("Tests completed");
	}

	public void RunWithCoverage(string categoryFilter, string componentFilter)
	{
		var coverageDir = _ctx.TemporaryDirectory / "coverage";
		coverageDir.CreateOrCleanDirectory();

		var filter = BuildFilter(categoryFilter, componentFilter);

		Log.Information("Running tests with coverage collection");
		DotNetTasks.DotNetTest(s => s
			.SetProjectFile(_ctx.Solution.GetProject("NtoLib.Test"))
			.SetConfiguration(_ctx.Configuration)
			.SetNoBuild(true)
			.SetNoRestore(true)
			.SetDataCollector("XPlat Code Coverage")
			.When(!string.IsNullOrWhiteSpace(filter), x => x.SetFilter(filter))
			.SetResultsDirectory(coverageDir));

		var coverageFile = coverageDir.GlobFiles("**/coverage.cobertura.xml").FirstOrDefault();
		if (coverageFile == null)
		{
			Log.Warning("No coverage file found");
			return;
		}

		var htmlDir = coverageDir / "html";
		Log.Information("Generating coverage report");

		DotNetTasks.DotNet(
			$"tool run reportgenerator -reports:{coverageFile} -targetdir:{htmlDir} -reporttypes:Html;TextSummary");

		var summaryFile = htmlDir / "Summary.txt";
		if (summaryFile.FileExists())
		{
			Log.Information("Coverage Summary:");
			var lines = File.ReadAllLines(summaryFile).Where(l => !string.IsNullOrWhiteSpace(l));
			foreach (var line in lines)
				Log.Information("  {Line}", line);
		}

		Log.Information("Full report: {Report}", htmlDir / "index.html");
	}

	private static string? BuildFilter(string testCategory, string testComponent)
	{
		var filters = new List<string>();

		if (!string.Equals(testCategory, "All", StringComparison.OrdinalIgnoreCase))
			filters.Add($"Category={testCategory}");

		if (!string.Equals(testComponent, "All", StringComparison.OrdinalIgnoreCase))
			filters.Add($"Component={testComponent}");

		return filters.Count > 0 ? string.Join("&", filters) : null;
	}
}
