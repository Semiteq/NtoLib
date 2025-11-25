using System;

using BuildScripts.Infrastructure;

using Nuke.Common;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities;

using Serilog;

namespace BuildScripts.Services;

public sealed class MsBuildService
{
	private readonly BuildContext _ctx;

	public MsBuildService(BuildContext ctx)
	{
		_ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
	}

	public void Restore()
	{
		Log.Information("Restoring NuGet packages");
		MSBuildTasks.MSBuild(s => MSBuildSettingsExtensions
			.SetTargetPath<MSBuildSettings>(s, _ctx.Solution)
			.SetTargets("Restore")
			.SetMaxCpuCount(Environment.ProcessorCount)
			.SetNodeReuse(true)
			.SetVerbosity(ToMsBuildVerbosity(NukeBuild.Verbosity)));
	}

	public void BuildSolution()
	{
		Log.Information("Compiling solution (Configuration: {Configuration})", _ctx.Configuration);

		MSBuildTasks.MSBuild(s => MSBuildSettingsExtensions
			.SetTargetPath<MSBuildSettings>(s, _ctx.Solution)
			.SetTargets("Build")
			.SetConfiguration(_ctx.Configuration)
			.SetMaxCpuCount(Environment.ProcessorCount)
			.SetNodeReuse(true)
			.SetVerbosity(ToMsBuildVerbosity(NukeBuild.Verbosity))
			.SetProperty("BuildInParallel", "true")
			.When(_ctx.Configuration == Configuration.Release, rs => MSBuildSettingsExtensions
				.SetProperty(rs, "Optimize", "true")
				.SetProperty("DebugType", "pdbonly")
				.SetProperty("DebugSymbols", "true")
				.SetProperty("DefineConstants", "TRACE"))
			.When(_ctx.Configuration == Configuration.Debug, ds => MSBuildSettingsExtensions
				.SetProperty(ds, "Optimize", "false")
				.SetProperty("DebugType", "full")
				.SetProperty("DebugSymbols", "true")
				.SetProperty("DefineConstants", "\"DEBUG;TRACE\"")));
	}

	private static MSBuildVerbosity ToMsBuildVerbosity(Verbosity verbosity)
	{
		return verbosity switch
		{
			Verbosity.Quiet => MSBuildVerbosity.Quiet,
			Verbosity.Minimal => MSBuildVerbosity.Minimal,
			Verbosity.Normal => MSBuildVerbosity.Minimal,
			Verbosity.Verbose => MSBuildVerbosity.Detailed,
			_ => MSBuildVerbosity.Minimal
		};
	}
}
