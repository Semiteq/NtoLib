using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Deploy);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    [Solution] 
    readonly Solution Solution;
    
    Project NtoLibProject => Solution.GetProject("NtoLib");

    AbsolutePath SolutionDirectory => Solution.Directory;
    AbsolutePath IlRepackExecutable => SolutionDirectory / "packages" / "ILRepack.2.0.40" / "tools" / "ilrepack.exe";
    AbsolutePath DestinationDirectory => @"C:\Program Files (x86)\MPSSoft\MasterSCADA";
    AbsolutePath NtoLibTableConfigSource => NtoLibProject.Directory / "NtoLibTableConfig";
    AbsolutePath ArtifactsDirectory => RootDirectory / "Releases";
    AbsolutePath NtoLibRegBat => NtoLibProject.Directory / "NtoLib_reg.bat";

    /// <summary>
    /// Maps Nuke's Verbosity to MSBuild's MSBuildVerbosity.
    /// </summary>
    private MSBuildVerbosity GetMSBuildVerbosity()
    {
        return Verbosity switch
        {
            Verbosity.Quiet => MSBuildVerbosity.Quiet,
            Verbosity.Minimal => MSBuildVerbosity.Minimal,
            Verbosity.Normal => MSBuildVerbosity.Normal,
            Verbosity.Verbose => MSBuildVerbosity.Detailed,
            _ => MSBuildVerbosity.Minimal
        };
    }

    /// <summary>
    /// Checks if the current verbosity level should output detailed logs.
    /// </summary>
    private bool IsVerboseLogging => Verbosity == Verbosity.Verbose;

    /// <summary>
    /// Writes a log message to console, optionally only in verbose mode.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="onlyVerbose">If true, logs only when verbosity is Detailed or Diagnostic.</param>
    private void Log(string message, bool onlyVerbose = false)
    {
        if (onlyVerbose && !IsVerboseLogging)
        {
            return;
        }
            
        Console.WriteLine(message);
    }

    /// <summary>
    /// Recursively copies a directory and its contents to a new location.
    /// </summary>
    /// <param name="source">The source directory path.</param>
    /// <param name="destination">The destination directory path.</param>
    private void CopyDirectoryRecursively(AbsolutePath source, AbsolutePath destination)
    {
        destination.CreateDirectory();
        
        var files = Directory.GetFiles(source);
        Log($"Found {files.Length} files in {source}", onlyVerbose: true);
        
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var destinationFile = destination / fileName;
            File.Copy(file, destinationFile, overwrite: true);
            Log($"  ✓ Copied file: {fileName}", onlyVerbose: true);
        }
        
        var directories = Directory.GetDirectories(source);
        Log($"Found {directories.Length} subdirectories in {source}", onlyVerbose: true);
        
        foreach (var directory in directories)
        {
            var directoryName = Path.GetFileName(directory);
            Log($"  → Processing directory: {directoryName}", onlyVerbose: true);
            CopyDirectoryRecursively(directory, destination / directoryName);
        }
    }
    
    /// <summary>
    /// Extracts assembly version from AssemblyInfo.cs file.
    /// </summary>
    /// <returns>Assembly version string or "1.0.0" if not found.</returns>
    private string GetAssemblyVersion()
    {
        var assemblyInfoPath = NtoLibProject.Directory / "Properties" / "AssemblyInfo.cs";
        Log($"Reading version from: {assemblyInfoPath}", onlyVerbose: true);
        
        var assemblyInfoContent = File.ReadAllText(assemblyInfoPath);
        var match = Regex.Match(assemblyInfoContent, @"\[assembly: AssemblyInformationalVersion\(""([^""]+)""\)\]");
        
        if (match.Success)
        {
            var version = match.Groups[1].Value;
            Log($"Detected version: {version}", onlyVerbose: true);
            return version;
        }
        
        Log("Version not found, using default: 1.0.0", onlyVerbose: true);
        return "1.0.0";
    }

    /// <summary>
    /// Checks if source files have been modified since the last compilation.
    /// </summary>
    /// <param name="outputDllPath">Path to the compiled DLL.</param>
    /// <returns>True if sources are newer than DLL, false otherwise.</returns>
    private bool AreSourcesModified(AbsolutePath outputDllPath)
    {
        if (!outputDllPath.FileExists())
        {
            return true;
        }

        var dllTimestamp = File.GetLastWriteTime(outputDllPath);
        Log($"Current DLL timestamp: {dllTimestamp}", onlyVerbose: true);
        
        var sourceFiles = NtoLibProject.Directory.GlobFiles("**/*.cs").ToArray();
        Log($"Found {sourceFiles.Length} source files to check", onlyVerbose: true);
        
        var modifiedFiles = sourceFiles.Where(file => File.GetLastWriteTime(file) > dllTimestamp).ToArray();
        
        if (modifiedFiles.Length == 0)
        {
            return false;
        }

        Log($"Found {modifiedFiles.Length} modified files since last build:", onlyVerbose: true);
        foreach (var file in modifiedFiles)
        {
            Log($"  - {file.Name} ({File.GetLastWriteTime(file)})", onlyVerbose: true);
        }

        return true;
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            Log($"=== Cleaning with verbosity: {Verbosity} ===");
            
            var directoriesToClean = SolutionDirectory.GlobDirectories("**/bin", "**/obj")
                .Where(directory => !directory.ToString().Contains("packages"))
                .ToArray();
            
            Log($"Found {directoriesToClean.Length} directories to clean", onlyVerbose: true);
                
            foreach (var directory in directoriesToClean)
            {
                if (directory.DirectoryExists())
                {
                    Log($"Deleting: {directory}", onlyVerbose: true);
                    directory.DeleteDirectory();
                }
            }
            
            ArtifactsDirectory.CreateOrCleanDirectory();
            Log($"Cleaned artifacts directory: {ArtifactsDirectory}");
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log("=== Restoring NuGet packages ===");
            
            MSBuildTasks.MSBuild(settings => MSBuildSettingsExtensions
                .SetTargetPath<MSBuildSettings>(settings, Solution)
                .SetTargets("Restore")
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(true)
                .SetVerbosity(GetMSBuildVerbosity()));
                
            Log("✓ Restore completed");
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log($"=== Compiling (Configuration: {Configuration}) ===");
            
            var targetDirectory = NtoLibProject.Directory / "bin" / Configuration;
            var outputDll = targetDirectory / $"{NtoLibProject.Name}.dll";
            
            if (AreSourcesModified(outputDll))
            {
                Log("Sources have been modified, compilation required");
            }
            else
            {
                Log("⚡ No source changes detected, MSBuild will use incremental compilation");
            }

            MSBuildTasks.MSBuild(settings => MSBuildSettingsExtensions
                .SetTargetPath<MSBuildSettings>(settings, Solution)
                .SetTargets("Build")
                .SetConfiguration(Configuration)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(true)
                .SetVerbosity(GetMSBuildVerbosity())
                .SetProperty("BuildInParallel", "true")
                .When(s => Configuration == Configuration.Release, releaseSettings => releaseSettings
                    .SetProperty("Optimize", "true")
                    .SetProperty("DebugType", "pdbonly")
                    .SetProperty("DebugSymbols", "true")
                    .SetProperty("DefineConstants", "TRACE"))
                .When(s => Configuration == Configuration.Debug, debugSettings => debugSettings
                    .SetProperty("Optimize", "false")
                    .SetProperty("DebugType", "full")
                    .SetProperty("DebugSymbols", "true")
                    .SetProperty("DefineConstants", "DEBUG;TRACE")));
                    
            Log("✓ Compilation completed");
        });

    Target IlRepack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log("=== Running ILRepack ===");
            
            var targetDirectory = NtoLibProject.Directory / "bin" / Configuration;
            var targetPath = targetDirectory / $"{NtoLibProject.Name}.dll";
            var outputPath = targetDirectory / "NtoLib.dll";

            Log($"Target directory: {targetDirectory}");
            Log($"Main assembly: {targetPath}", onlyVerbose: true);

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
                targetDirectory / "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
                targetDirectory / "Polly.dll",
                targetDirectory / "Serilog.Extensions.Logging.dll",
                targetDirectory / "Serilog.Sinks.Console.dll",
                targetDirectory / "Serilog.dll",
                targetDirectory / "Serilog.Sinks.File.dll",
                targetDirectory / "Serilog.Sinks.Debug.dll", 
                targetDirectory / "System.Diagnostics.DiagnosticSource.dll",
            };

            var existingAssemblies = assemblyPaths.Where(assembly => assembly.FileExists()).ToArray();
            
            Log($"Found {existingAssemblies.Length} assemblies to merge:");
            
            foreach (var assembly in existingAssemblies)
            {
                var fileInfo = new FileInfo(assembly);
                Log($"  ✓ {assembly.Name} ({fileInfo.Length / 1024} KB)", onlyVerbose: true);
            }

            if (existingAssemblies.Length == 0)
            {
                throw new InvalidOperationException("No assemblies found to merge.");
            }

            var arguments = new[]
            {
                "/target:library",
                $"/out:\"{outputPath}\"",
            }.Concat(existingAssemblies.Select(assembly => $"\"{assembly}\""));

            if (IsVerboseLogging)
            {
                Log($"ILRepack command: {IlRepackExecutable} {string.Join(" ", arguments)}", onlyVerbose: true);
            }

            var process = ProcessTasks.StartProcess(
                IlRepackExecutable,
                string.Join(" ", arguments),
                workingDirectory: targetDirectory,
                logOutput: IsVerboseLogging);
                
            process.AssertZeroExitCode();
            
            var outputFileInfo = new FileInfo(outputPath);
            Log($"✓ ILRepack completed: {outputPath.Name} ({outputFileInfo.Length / 1024} KB)");
        });

    Target CopyFiles => _ => _
        .DependsOn(IlRepack) 
        .Executes(() =>
        {
            Log("=== Copying files to destination ===");
            
            var targetDirectory = NtoLibProject.Directory / "bin" / Configuration;
            var targetFileName = $"{NtoLibProject.Name}.dll";
            
            Log($"Source: {targetDirectory}");
            Log($"Destination: {DestinationDirectory}");
            
            DestinationDirectory.CreateDirectory();

            var filesToCopy = new[]
            {
                ("System.Resources.Extensions.dll", targetDirectory / "System.Resources.Extensions.dll"),
                (targetFileName, targetDirectory / targetFileName),
                ("NtoLib.pdb", targetDirectory / "NtoLib.pdb")
            };

            var copiedCount = 0;
            var skippedCount = 0;
            
            foreach (var (destinationFileName, sourcePath) in filesToCopy)
            {
                if (sourcePath.FileExists())
                {
                    var destinationPath = DestinationDirectory / destinationFileName;
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    
                    var fileInfo = new FileInfo(sourcePath);
                    Log($"✓ Copied: {destinationFileName} ({fileInfo.Length / 1024} KB)", onlyVerbose: true);
                    copiedCount++;
                }
                else
                {
                    Log($"✗ Not found: {sourcePath}", onlyVerbose: true);
                    skippedCount++;
                }
            }
            
            Log($"Files copied: {copiedCount}, skipped: {skippedCount}");

            if (NtoLibTableConfigSource.DirectoryExists())
            {
                var configDestination = DestinationDirectory / "NtoLibTableConfig";
                
                if (configDestination.DirectoryExists())
                {
                    Log($"Removing old config: {configDestination}", onlyVerbose: true);
                    configDestination.DeleteDirectory();
                }
                    
                Log($"Copying config directory...", onlyVerbose: true);
                CopyDirectoryRecursively(NtoLibTableConfigSource, configDestination);
                Log("✓ NtoLibTableConfig folder copied");
            }
            else
            {
                Log($"✗ Config folder not found: {NtoLibTableConfigSource}");
            }
            
            Log("✓ Copy operation completed");
        });

    Target Archive => _ => _
        .DependsOn(CopyFiles)
        .Executes(() =>
        {
            Log("=== Creating archive ===");
            
            var version = GetAssemblyVersion();
            var archiveName = $"NtoLib_v{version}.zip";
            var archivePath = ArtifactsDirectory / archiveName;
            
            Log($"Archive: {archiveName}");

            var tempArchiveDir = TemporaryDirectory / "archive";
            tempArchiveDir.CreateOrCleanDirectory();

            var sourceDir = NtoLibProject.Directory / "bin" / Configuration;

            var filesToArchive = new[]
            {
                "NtoLib.dll",
                "System.Resources.Extensions.dll"
            };

            var archivedCount = 0;
            
            foreach (var fileName in filesToArchive)
            {
                var sourcePath = sourceDir / fileName;
                if (sourcePath.FileExists())
                {
                    File.Copy(sourcePath, tempArchiveDir / fileName, overwrite: true);
                    Log($"  + {fileName}", onlyVerbose: true);
                    archivedCount++;
                }
            }
            
            Log($"Added {archivedCount} files to archive", onlyVerbose: true);
            
            if (NtoLibTableConfigSource.DirectoryExists())
            {
                Log("Adding NtoLibTableConfig...", onlyVerbose: true);
                CopyDirectoryRecursively(NtoLibTableConfigSource, tempArchiveDir / "NtoLibTableConfig");
            }

            if (NtoLibRegBat.FileExists())
            {
                File.Copy(NtoLibRegBat, tempArchiveDir / "NtoLib_reg.bat", overwrite: true);
                Log("  + NtoLib_reg.bat", onlyVerbose: true);
            }
            else
            {
                Log($"✗ Warning: NtoLib_reg.bat not found");
            }
            
            if (archivePath.FileExists())
            {
                archivePath.DeleteFile();
            }

            ZipFile.CreateFromDirectory(tempArchiveDir, archivePath);
            
            var archiveInfo = new FileInfo(archivePath);
            Log($"✓ Archive created: {archivePath.Name} ({archiveInfo.Length / 1024} KB)");
        });
        
    Target Deploy => _ => _
        .DependsOn(Archive)
        .Executes(() =>
        {
            Log("=== Build completed successfully! ===");
            Log($"Configuration: {Configuration}");
            Log($"Destination: {DestinationDirectory}");
            Log($"Artifacts: {ArtifactsDirectory}");
        });
}