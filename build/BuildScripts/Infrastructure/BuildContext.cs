using System;
using System.IO;
using System.Text.RegularExpressions;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

using Serilog;

namespace BuildScripts.Infrastructure;

public sealed class BuildContext
{
    public BuildContext(
        NukeBuild build,
        Solution solution,
        Configuration configuration,
        AbsolutePath destinationDirectory)
    {
        Build = build ?? throw new ArgumentNullException(nameof(build));
        Solution = solution ?? throw new ArgumentNullException(nameof(solution));
        Configuration = configuration;
        DestinationDirectory = destinationDirectory;

        SolutionDirectory = Solution.Directory;
        RootDirectory = NukeBuild.RootDirectory;
        TemporaryDirectory = NukeBuild.TemporaryDirectory;
        ArtifactsDirectory = RootDirectory / "Releases";

        NtoLibProject = Solution.GetProject("NtoLib")
                        ?? throw new InvalidOperationException("Project 'NtoLib' not found in solution.");

        IlRepackExecutable = SolutionDirectory / "packages" / "ILRepack.2.0.40" / "tools" / "ILRepack.exe";
        NtoLibTableConfigSource = NtoLibProject.Directory / "NtoLibTableConfig";
        NtoLibRegBat = NtoLibProject.Directory / "NtoLib_reg.bat";
    }

    public NukeBuild Build { get; }
    public Solution Solution { get; }
    public Project NtoLibProject { get; }
    public Configuration Configuration { get; }
    public AbsolutePath DestinationDirectory { get; }

    public AbsolutePath RootDirectory { get; }
    public AbsolutePath? SolutionDirectory { get; }
    public AbsolutePath TemporaryDirectory { get; }
    public AbsolutePath ArtifactsDirectory { get; }

    public AbsolutePath IlRepackExecutable { get; }
    public AbsolutePath NtoLibTableConfigSource { get; }
    public AbsolutePath NtoLibRegBat { get; }

    public AbsolutePath TargetDirectory => NtoLibProject.Directory / "bin" / Configuration;
    public AbsolutePath MergedDll => TargetDirectory / "NtoLib.dll";
    public AbsolutePath OriginalDll => TargetDirectory / $"{NtoLibProject.Name}.dll";

    public string[] FilesToDeploy => new[]
    {
        "NtoLib.dll",
        "NtoLib.pdb",
        "System.Resources.Extensions.dll"
    };

    public string[] AssembliesToMerge => new[]
    {
        "System.Collections.Immutable.dll",
        "System.Diagnostics.DiagnosticSource.dll",

        "Microsoft.Bcl.HashCode.dll",
        "Microsoft.Bcl.TimeProvider.dll",
        "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
        "Microsoft.Extensions.DependencyInjection.dll",
        "Microsoft.Extensions.Logging.Abstractions.dll",
        "Microsoft.Extensions.Logging.dll",
        "Microsoft.Extensions.Options.dll",
        "Microsoft.Extensions.Primitives.dll",

        "Serilog.dll",
        "Serilog.Sinks.Console.dll",
        "Serilog.Sinks.Debug.dll",
        "Serilog.Sinks.File.dll",
        "Serilog.Extensions.Logging.dll",

        "Polly.dll",
        "Polly.Core.dll",

        "OneOf.dll",
        "FluentResults.dll",
        "CsvHelper.dll",
        "YamlDotNet.dll",
        "EasyModbus.dll",
        "COMDeviceSDK.dll",

        "AngouriMath.dll",
        "Antlr4.Runtime.Standard.dll",
        "GenericTensor.dll",
        "HonkSharp.dll",
        "Numbers.dll",
    };

    public string ReadAssemblyInformationalVersion()
    {
        var assemblyInfoPath = NtoLibProject.Directory / "Properties" / "AssemblyInfo.cs";
        if (!assemblyInfoPath.FileExists())
        {
            Log.Warning("AssemblyInfo.cs not found, using default version: 1.0.0");
            return "1.0.0";
        }

        var content = File.ReadAllText(assemblyInfoPath);
        var match = Regex.Match(content, @"\[assembly:\s*AssemblyInformationalVersion\(""([^""]+)""\)\]");
        
        if (match.Success)
            return match.Groups[1].Value;

        Log.Warning("Version not found in AssemblyInfo.cs, using default: 1.0.0");
        return "1.0.0";
    }
}