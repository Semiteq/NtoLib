using System;
using System.IO;
using System.Linq;

using Build.BuildScripts.Infrastructure;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

using Serilog;

namespace Build.BuildScripts.Services;

public sealed class IlRepackService
{
	private readonly BuildContext _ctx;

	public IlRepackService(BuildContext ctx)
	{
		_ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
	}

	public void Run()
	{
		if (!_ctx.IlRepackExecutable.FileExists())
			throw new FileNotFoundException("ILRepack executable not found.", _ctx.IlRepackExecutable);

		var targetPath = _ctx.OriginalDll;
		var outputPath = _ctx.MergedDll;

		var assemblies = new[] { targetPath }
			.Concat(_ctx.AssembliesToMerge.Select(a => _ctx.TargetDirectory / a))
			.Where(p => p.FileExists())
			.ToArray();

		if (assemblies.Length <= 1)
			throw new InvalidOperationException($"No assemblies found to merge in '{_ctx.TargetDirectory}'.");

		Log.Information("Merging {Count} assemblies into {Output}", assemblies.Length, outputPath.Name);

		foreach (var a in assemblies)
		{
			var fi = new FileInfo(a);
			Log.Debug("  → {Name} ({Size} KB)", a.Name, fi.Length / 1024);
		}

		var args = new[]
		{
			"/target:library",
			$"/out:\"{outputPath}\""
		}.Concat(assemblies.Select(a => $"\"{a}\""));

		var process = ProcessTasks.StartProcess(
			_ctx.IlRepackExecutable,
			string.Join(" ", args),
			workingDirectory: _ctx.TargetDirectory,
			logOutput: NukeBuild.Verbosity >= Verbosity.Verbose);

		process.AssertZeroExitCode();

		var outInfo = new FileInfo(outputPath);
		Log.Information("ILRepack completed: {File} ({Size} KB)", outputPath.Name, outInfo.Length / 1024);
	}
}
