using System;
using System.IO;
using System.Linq;

using BuildScripts.Infrastructure;
using BuildScripts.Services;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

using Serilog;

class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.BuildDebug);

	[Parameter("Configuration to build - Default is 'Release'")]
	public Configuration Configuration { get; set; } = Configuration.Debug;

	[Parameter("Destination directory for local development deployment")]
	public AbsolutePath DestinationDirectory { get; set; } = @"C:\Program Files (x86)\MPSSoft\MasterSCADA";

	[Parameter("Test category filter: All (default), Integration, Unit")]
	public string TestCategory { get; set; } = "All";

	[Parameter("Test component filter: All (default), ConfigLoader, FormulaPrecompiler")]
	public string TestComponent { get; set; } = "All";

	[Solution]
	public Solution Solution { get; set; }

	BuildContext Ctx => _ctx ??= new BuildContext(this, Solution, Configuration, DestinationDirectory);
	BuildContext? _ctx;

	MsBuildService MsBuild => _ms ??= new MsBuildService(Ctx);
	MsBuildService? _ms;

	IlRepackService IlRepackSvc => _il ??= new IlRepackService(Ctx);
	IlRepackService? _il;

	DeploymentService Deploy => _dep ??= new DeploymentService(Ctx);
	DeploymentService? _dep;

	PackagingService Pack => _pack ??= new PackagingService(Ctx);
	PackagingService? _pack;

	TestService Tests => _tests ??= new TestService(Ctx);
	TestService? _tests;

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			Log.Information("Cleaning (Configuration: {Configuration})", Configuration);

			var dirs = Ctx.SolutionDirectory.GlobDirectories("**/bin", "**/obj")
				.Where(d => !d.ToString().Contains("packages"))
				.Where(d => !d.ToString().Contains("build"))
				.ToArray();

			foreach (var d in dirs)
			{
				if (!d.DirectoryExists())
					continue;

				try
				{
					d.DeleteDirectory();
				}
				catch (UnauthorizedAccessException ex)
				{
					Log.Warning("Cannot delete {Dir}: {Msg}", d, ex.Message);
				}
				catch (IOException ex)
				{
					Log.Warning("Cannot delete {Dir}: {Msg}", d, ex.Message);
				}
			}

			Ctx.ArtifactsDirectory.CreateDirectory();
			Log.Information("Artifacts directory ready: {Dir}", Ctx.ArtifactsDirectory);
		});

	Target Restore => _ => _
		.Executes(() =>
		{
			MsBuild.Restore();
			Log.Information("Restore completed");
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			MsBuild.BuildSolution();
			Log.Information("Compilation completed");
		});

	Target ILRepack => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			IlRepackSvc.Run();
		});

	Target CopyToLocal => _ => _
		.DependsOn(ILRepack)
		.OnlyWhenDynamic(() => Configuration == Configuration.Debug)
		.Executes(() =>
		{
			Log.Information("Copying files to local deployment directory: {Dest}", Ctx.DestinationDirectory);

			if (Deploy.AreFilesUpToDate())
				return;

			Deploy.CopyToLocal();
			Log.Information("Local deployment completed");
		});

	Target PackageArchive => _ => _
		.DependsOn(ILRepack)
		.OnlyWhenDynamic(() => Configuration == Configuration.Release)
		.Executes(() =>
		{
			Log.Information("Creating release archive");
			var archive = Pack.CreateReleaseArchive();
		});

	Target Test => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			Log.Information("Running tests (Category: {Category}, Component: {Component})", TestCategory,
				TestComponent);
			Tests.RunTests(TestCategory, TestComponent, Verbosity == Verbosity.Verbose);
		});

	Target TestWithCoverage => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			Tests.RunWithCoverage(TestCategory, TestComponent);
		});

	Target BuildDebug => _ => _
		.Description("Build Debug configuration and copy to local deployment directory")
		.DependsOn(Clean)
		.DependsOn(CopyToLocal)
		.Executes(() =>
		{
			Log.Information("Debug build completed successfully");
			Log.Information("Deployed to: {Destination}", Ctx.DestinationDirectory);
		});

	Target BuildRelease => _ => _
		.Description("Build Release configuration without packaging")
		.DependsOn(Clean)
		.DependsOn(Test)
		.DependsOn(ILRepack)
		.Executes(() =>
		{
			Log.Information("Release build completed successfully");
			Log.Information("Output: {OutputPath}", Ctx.MergedDll);
		});

	Target Package => _ => _
		.Description("Build Release configuration and create deployment archive")
		.DependsOn(Clean)
		.DependsOn(TestWithCoverage)
		.DependsOn(PackageArchive)
		.Executes(() =>
		{
			var version = Ctx.ReadAssemblyInformationalVersion();
			Log.Information("Package build completed successfully");
			Log.Information("Version: {Version}", version);
			Log.Information("Artifacts: {Dir}", Ctx.ArtifactsDirectory);
		});
}
