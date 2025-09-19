using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Deploy);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    [Solution] readonly Solution Solution;
    
    Project NtoLibProject => Solution.GetProject("NtoLib");

    AbsolutePath SolutionDirectory => Solution.Directory;
    AbsolutePath IlRepackExecutable => SolutionDirectory / "packages" / "ILRepack.2.0.40" / "tools" / "ilrepack.exe";
    AbsolutePath DestinationDirectory => @"C:\Program Files (x86)\MPSSoft\MasterSCADA";
    
    AbsolutePath NtoLibTableConfigSource => NtoLibProject.Directory / "NtoLibTableConfig";
    
    AbsolutePath ArtifactsDirectory => RootDirectory / "Releases";

    AbsolutePath NtoLibRegBat => NtoLibProject.Directory / "NtoLib_reg.bat";

    /// <summary>
    /// Recursively copies a directory and its contents to a new location.
    /// </summary>
    /// <param name="source">The source directory path.</param>
    /// <param name="destination">The destination directory path.</param>
    private static void CopyDirectoryRecursively(AbsolutePath source, AbsolutePath destination)
    {
        destination.CreateDirectory();
        
        // Copy all files
        foreach (var file in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, destination / fileName, overwrite: true);
        }
        
        // Recursively copy subdirectories
        foreach (var directory in Directory.GetDirectories(source))
        {
            var dirName = Path.GetFileName(directory);
            CopyDirectoryRecursively(directory, destination / dirName);
        }
    }
    
    private string GetAssemblyVersion()
    {
        var assemblyInfoContent = File.ReadAllText(NtoLibProject.Directory / "Properties" / "AssemblyInfo.cs");
        var match = Regex.Match(assemblyInfoContent, @"\[assembly: AssemblyInformationalVersion\(""([^""]+)""\)\]");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return "1.0.0";
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            var dirsToClean = SolutionDirectory.GlobDirectories("**/bin", "**/obj")
                .Where(x => !x.ToString().Contains("packages"));
                
            foreach (var dir in dirsToClean)
            {
                if (dir.DirectoryExists())
                    dir.DeleteDirectory();
            }
            
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Restore")
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(true));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Rebuild")
                .SetConfiguration(Configuration)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(true)
                .When(s => Configuration == Configuration.Release, x => x
                    .SetProperty("Optimize", "true")
                    .SetProperty("DebugType", "pdbonly")
                    .SetProperty("DebugSymbols", "true")
                    .SetProperty("DefineConstants", "TRACE"))
                .When(s => Configuration == Configuration.Debug, x => x
                    .SetProperty("Optimize", "false")
                    .SetProperty("DebugType", "full")
                    .SetProperty("DebugSymbols", "true")
                    .SetProperty("DefineConstants", "DEBUG;TRACE")));
        });

    Target IlRepack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var targetDirectory = NtoLibProject.Directory / "bin" / Configuration;
            var targetPath = targetDirectory / $"{NtoLibProject.Name}.dll";
            var outputPath = targetDirectory / "NtoLib.dll";

            Console.WriteLine($"Target directory: {targetDirectory}");
            Console.WriteLine($"Looking for main assembly: {targetPath}");

            var assemblyPaths = new[]
            {
                targetPath,
                targetDirectory / "COMDeviceSDK.dll",
                targetDirectory / "EasyModbus.dll", 
                targetDirectory / "OneOf.dll",
                targetDirectory / "System.Collections.Immutable.dll",
                targetDirectory / "CsvHelper.dll",
                targetDirectory / "FluentResults.dll",
                targetDirectory / "YamlDotNet.dll",
                targetDirectory / "Microsoft.Extensions.Logging.Abstractions.dll",
                targetDirectory / "Microsoft.Extensions.DependencyInjection.dll",
                targetDirectory / "Microsoft.Extensions.DependencyInjection.Abstractions.dll"
            };

            var existingAssemblies = assemblyPaths.Where(x => x.FileExists()).ToArray();
            
            Console.WriteLine($"Found {existingAssemblies.Length} assemblies to merge.");

            if (existingAssemblies.Length == 0)
            {
                throw new InvalidOperationException("No assemblies found to merge.");
            }

            var arguments = new[]
            {
                "/target:library",
                $"/out:\"{outputPath}\"",
            }.Concat(existingAssemblies.Select(a => $"\"{a}\""));

            var process = ProcessTasks.StartProcess(
                IlRepackExecutable,
                string.Join(" ", arguments),
                workingDirectory: targetDirectory);
                
            process.AssertZeroExitCode();
            
            Console.WriteLine($"ILRepack completed successfully. Output: {outputPath}");
        });

    Target CopyFiles => _ => _
        .DependsOn(IlRepack) 
        .Executes(() =>
        {
            var targetDirectory = NtoLibProject.Directory / "bin" / Configuration;
            var targetFileName = $"{NtoLibProject.Name}.dll";
            
            Console.WriteLine($"Source directory for files: {targetDirectory}");
            Console.WriteLine($"Destination directory: {DestinationDirectory}");
            
            DestinationDirectory.CreateDirectory();

            // Copy main files
            var filesToCopy = new[]
            {
                ("System.Resources.Extensions.dll", targetDirectory / "System.Resources.Extensions.dll"),
                (targetFileName, targetDirectory / targetFileName),
                ("NtoLib.pdb", targetDirectory / "NtoLib.pdb")
            };

            Console.WriteLine("Copying files to destination:");
            foreach (var (destName, sourcePath) in filesToCopy)
            {
                if (sourcePath.FileExists())
                {
                    var destPath = DestinationDirectory / destName;
                    File.Copy(sourcePath, destPath, overwrite: true);
                    Console.WriteLine($"✓ Copied: {sourcePath} to {destPath}");
                }
                else
                {
                    Console.WriteLine($"✗ File not found, skipping: {sourcePath}");
                }
            }

            // Copy NtoLibTableConfig folder
            Console.WriteLine($"Checking config folder: {NtoLibTableConfigSource}");
            if (NtoLibTableConfigSource.DirectoryExists())
            {
                var configDestination = DestinationDirectory / "NtoLibTableConfig";
                
                if (configDestination.DirectoryExists())
                {
                    Console.WriteLine($"Deleting existing config directory: {configDestination}");
                    configDestination.DeleteDirectory();
                }
                    
                Console.WriteLine($"Copying config directory from {NtoLibTableConfigSource} to {configDestination}");
                CopyDirectoryRecursively(NtoLibTableConfigSource, configDestination);
                Console.WriteLine("✓ NtoLibTableConfig folder copied successfully");
            }
            else
            {
                Console.WriteLine($"✗ NtoLibTableConfig folder not found: {NtoLibTableConfigSource}");
            }
            
            Console.WriteLine("Copy operation completed.");
        });

    Target Archive => _ => _
        .DependsOn(CopyFiles)
        .Executes(() =>
        {
            var version = GetAssemblyVersion();
            var archiveName = $"NtoLib_v{version}.zip";
            var archivePath = ArtifactsDirectory / archiveName;
            
            Console.WriteLine($"Creating archive: {archivePath}");

            var tempArchiveDir = TemporaryDirectory / "archive";
            tempArchiveDir.CreateOrCleanDirectory();

            var sourceDir = NtoLibProject.Directory / "bin" / Configuration;

            var filesToArchive = new[]
            {
                "NtoLib.dll",
                "System.Resources.Extensions.dll"
            };

            foreach (var file in filesToArchive)
            {
                if ((sourceDir / file).FileExists())
                {
                    File.Copy(sourceDir / file, tempArchiveDir / file, overwrite: true);
                }
            }
            
            if (NtoLibTableConfigSource.DirectoryExists())
            {
                CopyDirectoryRecursively(NtoLibTableConfigSource, tempArchiveDir / "NtoLibTableConfig");
            }

            if (NtoLibRegBat.FileExists())
            {
                File.Copy(NtoLibRegBat, tempArchiveDir / "NtoLib_reg.bat", overwrite: true);
            }
            else
            {
                Console.WriteLine($"✗ Warning: NtoLib_reg.bat not found at {NtoLibRegBat}");
            }
            
            if (archivePath.FileExists())
            {
                archivePath.DeleteFile();
            }

            ZipFile.CreateFromDirectory(tempArchiveDir, archivePath);
            
            Console.WriteLine($"✓ Archive created successfully: {archivePath}");
        });
        
    Target Deploy => _ => _
        .DependsOn(Archive)
        .Executes(() =>
        {
            Console.WriteLine("Build and deployment completed successfully!");
            Console.WriteLine($"Configuration: {Configuration}");
            Console.WriteLine($"Destination: {DestinationDirectory}");
            Console.WriteLine($"Artifacts located at: {ArtifactsDirectory}");
        });
}