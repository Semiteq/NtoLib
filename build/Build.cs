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
using Serilog;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.BuildDebug);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    [Parameter("Destination directory for local development deployment")]
    readonly AbsolutePath DestinationDirectory = @"C:\Program Files (x86)\MPSSoft\MasterSCADA";

    [Solution] 
    readonly Solution Solution;
    
    Project NtoLibProject => Solution.GetProject("NtoLib");

    AbsolutePath SolutionDirectory => Solution.Directory;
    AbsolutePath IlRepackExecutable => SolutionDirectory / "packages" / "ILRepack.2.0.40" / "tools" / "ILRepack.exe";
    AbsolutePath NtoLibTableConfigSource => NtoLibProject.Directory / "NtoLibTableConfig";
    AbsolutePath ArtifactsDirectory => RootDirectory / "Releases";
    AbsolutePath NtoLibRegBat => NtoLibProject.Directory / "NtoLib_reg.bat";

    AbsolutePath GetTargetDirectory() => NtoLibProject.Directory / "bin" / Configuration;
    AbsolutePath GetMergedDll() => GetTargetDirectory() / "NtoLib.dll";
    AbsolutePath GetOriginalDll() => GetTargetDirectory() / $"{NtoLibProject.Name}.dll";

    static readonly string[] FilesToDeploy = 
    {
        "NtoLib.dll",
        "NtoLib.pdb",
        "System.Resources.Extensions.dll"
    };

    static readonly string[] AssembliesToMerge = 
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

    MSBuildVerbosity GetMSBuildVerbosity()
    {
        return Verbosity switch
        {
            Verbosity.Quiet => MSBuildVerbosity.Quiet,
            Verbosity.Minimal => MSBuildVerbosity.Minimal,
            Verbosity.Normal => MSBuildVerbosity.Minimal,
            Verbosity.Verbose => MSBuildVerbosity.Detailed,
            _ => MSBuildVerbosity.Minimal
        };
    }

    void CopyDirectoryRecursively(AbsolutePath source, AbsolutePath destination)
    {
        destination.CreateDirectory();
        
        foreach (var file in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, destination / fileName, overwrite: true);
            Log.Debug("Copied file: {FileName}", fileName);
        }
        
        foreach (var directory in Directory.GetDirectories(source))
        {
            var directoryName = Path.GetFileName(directory);
            CopyDirectoryRecursively(directory, destination / directoryName);
        }
    }
    
    string GetAssemblyVersion()
    {
        var assemblyInfoPath = NtoLibProject.Directory / "Properties" / "AssemblyInfo.cs";
        var assemblyInfoContent = File.ReadAllText(assemblyInfoPath);
        var match = Regex.Match(assemblyInfoContent, @"\[assembly: AssemblyInformationalVersion\(""([^""]+)""\)\]");
        
        if (match.Success)
        {
            var version = match.Groups[1].Value;
            Log.Debug("Detected version: {Version}", version);
            return version;
        }
        
        Log.Warning("Version not found in AssemblyInfo.cs, using default: 1.0.0");
        return "1.0.0";
    }

    bool IsMergedDllUpToDate()
    {
        var mergedDll = GetMergedDll();
        var markerFile = mergedDll.Parent / (mergedDll.Name + ".merged-marker");

        if (!mergedDll.FileExists() || !markerFile.FileExists())
        {
            Log.Debug("Merged DLL or marker file does not exist, ILRepack required");
            return false;
        }

        if (File.GetLastWriteTime(mergedDll) > File.GetLastWriteTime(markerFile))
        {
            Log.Debug("Main assembly is newer than the last merge marker, ILRepack is required");
            return false;
        }

        var mergedTimestamp = File.GetLastWriteTime(mergedDll);
        var targetDirectory = GetTargetDirectory();
        
        var inputAssemblies = new[] { GetOriginalDll() }
            .Concat(AssembliesToMerge.Select(name => targetDirectory / name))
            .Where(path => path.FileExists())
            .ToArray();

        foreach (var assembly in inputAssemblies)
        {
            if (File.GetLastWriteTime(assembly) > mergedTimestamp)
            {
                Log.Debug("Assembly {Name} is newer than merged DLL", Path.GetFileName(assembly));
                return false;
            }
        }

        Log.Information("Merged DLL is up to date, skipping ILRepack");
        return true;
    }

    bool AreFilesUpToDate(AbsolutePath sourceDir, AbsolutePath destDir)
    {
        if (!destDir.DirectoryExists())
        {
            return false;
        }

        foreach (var fileName in FilesToDeploy)
        {
            var sourceFile = sourceDir / fileName;
            var destFile = destDir / fileName;
            
            if (!sourceFile.FileExists())
            {
                continue;
            }

            if (!destFile.FileExists())
            {
                Log.Debug("File {FileName} missing in destination", fileName);
                return false;
            }

            if (File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile))
            {
                Log.Debug("File {FileName} is newer than destination", fileName);
                return false;
            }
        }

        var configSource = NtoLibTableConfigSource;
        var configDest = destDir / "NtoLibTableConfig";
        
        if (configSource.DirectoryExists() && configDest.DirectoryExists())
        {
            var sourceFiles = configSource.GlobFiles("**/*").ToArray();
            
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(configSource, sourceFile);
                var destFile = configDest / relativePath;
                
                if (!destFile.FileExists() || File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile))
                {
                    Log.Debug("Config file {FileName} requires update", relativePath);
                    return false;
                }
            }
        }
        else if (configSource.DirectoryExists())
        {
            Log.Debug("Config directory missing in destination");
            return false;
        }

        Log.Information("All files are up to date in destination");
        return true;
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            Log.Information("Cleaning solution (Configuration: {Configuration})", Configuration);
            
            var directoriesToClean = SolutionDirectory.GlobDirectories("**/bin", "**/obj")
                .Where(directory => !directory.ToString().Contains("packages"))
                .Where(directory => !directory.ToString().Contains("build"))
                .ToArray();
            
            Log.Debug("Found {Count} directories to clean", directoriesToClean.Length);
                
            foreach (var directory in directoriesToClean)
            {
                if (directory.DirectoryExists())
                {
                    try
                    {
                        Log.Debug("Deleting: {Directory}", directory);
                        directory.DeleteDirectory();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warning("Cannot delete {Directory}: {Message}", directory, ex.Message);
                    }
                    catch (IOException ex)
                    {
                        Log.Warning("Cannot delete {Directory}: {Message}", directory, ex.Message);
                    }
                }
            }
            
            ArtifactsDirectory.CreateOrCleanDirectory();
            Log.Information("Cleaned artifacts directory: {Directory}", ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log.Information("Restoring NuGet packages");
            
            MSBuildTasks.MSBuild(settings => MSBuildSettingsExtensions
                .SetTargetPath<MSBuildSettings>(settings, Solution)
                .SetTargets("Restore")
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(true)
                .SetVerbosity(GetMSBuildVerbosity()));
                
            Log.Information("Restore completed");
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Information("Compiling solution (Configuration: {Configuration})", Configuration);

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
                    .SetProperty("DefineConstants", "\"DEBUG;TRACE\"")));
                    
            Log.Information("Compilation completed");
        });

    Target ILRepack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            if (IsMergedDllUpToDate())
            {
                return;
            }

            Log.Information("Running ILRepack");
            
            var targetDirectory = GetTargetDirectory();
            var targetPath = GetOriginalDll();
            var outputPath = GetMergedDll();

            var assemblyPaths = new[] { targetPath }
                .Concat(AssembliesToMerge.Select(name => targetDirectory / name))
                .Where(assembly => assembly.FileExists())
                .ToArray();

            if (assemblyPaths.Length <= 1)
            {
                throw new InvalidOperationException($"No assemblies found to merge in {targetDirectory}");
            }
            
            Log.Information("Merging {Count} assemblies into {OutputFile}", assemblyPaths.Length, outputPath.Name);
            Log.Debug("Output: {OutputPath}", outputPath);
            
            foreach (var assembly in assemblyPaths)
            {
                var fileInfo = new FileInfo(assembly);
                Log.Debug("  → {Name} ({Size} KB)", assembly.Name, fileInfo.Length / 1024);
            }

            var arguments = new[]
            {
                "/target:library",
                $"/out:\"{outputPath}\"",
            }.Concat(assemblyPaths.Select(assembly => $"\"{assembly}\""));

            var process = ProcessTasks.StartProcess(
                IlRepackExecutable,
                string.Join(" ", arguments),
                workingDirectory: targetDirectory,
                logOutput: Verbosity >= Verbosity.Verbose);
                
            process.AssertZeroExitCode();
            
            var outputFileInfo = new FileInfo(outputPath);
            Log.Information("ILRepack completed: {FileName} ({Size} KB)", outputPath.Name, outputFileInfo.Length / 1024);
            
            var markerFile = outputPath.Parent / (outputPath.Name + ".merged-marker");
            File.WriteAllText(markerFile, DateTime.UtcNow.ToString("O"));
            Log.Debug("Created/updated merge marker file.");
        });

    Target CopyToLocal => _ => _
        .DependsOn(ILRepack)
        .OnlyWhenDynamic(() => Configuration == Configuration.Debug)
        .Executes(() =>
        {
            Log.Information("Copying files to local deployment directory");
            Log.Information("Destination: {Destination}", DestinationDirectory);
            
            var targetDirectory = GetTargetDirectory();

            if (AreFilesUpToDate(targetDirectory, DestinationDirectory))
            {
                return;
            }
            
            DestinationDirectory.CreateDirectory();

            var copiedCount = 0;
            
            foreach (var fileName in FilesToDeploy)
            {
                var sourcePath = targetDirectory / fileName;
                
                if (sourcePath.FileExists())
                {
                    var destinationPath = DestinationDirectory / fileName;
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    
                    var fileInfo = new FileInfo(sourcePath);
                    Log.Debug("Copied: {FileName} ({Size} KB)", fileName, fileInfo.Length / 1024);
                    copiedCount++;
                }
                else
                {
                    Log.Warning("File not found: {FileName}", fileName);
                }
            }
            
            Log.Information("Copied {Count} files", copiedCount);

            if (NtoLibTableConfigSource.DirectoryExists())
            {
                var configDestination = DestinationDirectory / "NtoLibTableConfig";
                
                if (configDestination.DirectoryExists())
                {
                    Log.Debug("Removing old config directory");
                    configDestination.DeleteDirectory();
                }
                    
                Log.Debug("Copying config directory");
                CopyDirectoryRecursively(NtoLibTableConfigSource, configDestination);
                Log.Information("Copied NtoLibTableConfig folder");
            }
            else
            {
                Log.Warning("Config folder not found: {ConfigPath}", NtoLibTableConfigSource);
            }
            
            Log.Information("Local deployment completed");
        });

    Target PackageArchive => _ => _
        .DependsOn(ILRepack)
        .OnlyWhenDynamic(() => Configuration == Configuration.Release)
        .Executes(() =>
        {
            Log.Information("Creating release archive");
            
            var version = GetAssemblyVersion();
            var archiveName = $"NtoLib_v{version}.zip";
            var archivePath = ArtifactsDirectory / archiveName;
            
            Log.Information("Archive: {ArchiveName}", archiveName);

            var tempArchiveDir = TemporaryDirectory / "archive";
            tempArchiveDir.CreateOrCleanDirectory();

            var sourceDir = GetTargetDirectory();

            var filesToArchive = new[] { "NtoLib.dll", "System.Resources.Extensions.dll" };
            var archivedCount = 0;
            
            foreach (var fileName in filesToArchive)
            {
                var sourcePath = sourceDir / fileName;
                if (sourcePath.FileExists())
                {
                    File.Copy(sourcePath, tempArchiveDir / fileName, overwrite: true);
                    Log.Debug("Added: {FileName}", fileName);
                    archivedCount++;
                }
                else
                {
                    Log.Warning("File not found for archive: {FileName}", fileName);
                }
            }
            
            Log.Debug("Added {Count} files to archive", archivedCount);
            
            if (NtoLibTableConfigSource.DirectoryExists())
            {
                Log.Debug("Adding NtoLibTableConfig");
                CopyDirectoryRecursively(NtoLibTableConfigSource, tempArchiveDir / "NtoLibTableConfig");
            }
            else
            {
                Log.Warning("Config folder not found: {ConfigPath}", NtoLibTableConfigSource);
            }

            if (NtoLibRegBat.FileExists())
            {
                File.Copy(NtoLibRegBat, tempArchiveDir / "NtoLib_reg.bat", overwrite: true);
                Log.Debug("Added: NtoLib_reg.bat");
            }
            else
            {
                Log.Warning("NtoLib_reg.bat not found: {BatPath}", NtoLibRegBat);
            }
            
            if (archivePath.FileExists())
            {
                archivePath.DeleteFile();
            }

            ZipFile.CreateFromDirectory(tempArchiveDir, archivePath);
            
            var archiveInfo = new FileInfo(archivePath);
            Log.Information("Archive created: {ArchiveName} ({Size} KB)", archivePath.Name, archiveInfo.Length / 1024);
            Log.Information("Location: {ArchivePath}", archivePath);
        });

    Target BuildDebug => _ => _
        .Description("Build Debug configuration and copy to local deployment directory")
        .DependsOn(Clean)
        .DependsOn(CopyToLocal)
        .Executes(() =>
        {
            Log.Information("Debug build completed successfully");
            Log.Information("Deployed to: {Destination}", DestinationDirectory);
        });

    Target BuildRelease => _ => _
        .Description("Build Release configuration without packaging")
        .DependsOn(Clean)
        .DependsOn(ILRepack)
        .Executes(() =>
        {
            Log.Information("Release build completed successfully");
            Log.Information("Output: {OutputPath}", GetMergedDll());
        });

    Target Package => _ => _
        .Description("Build Release configuration and create deployment archive")
        .DependsOn(Clean)
        .DependsOn(PackageArchive)
        .Executes(() =>
        {
            var version = GetAssemblyVersion();
            Log.Information("Package build completed successfully");
            Log.Information("Version: {Version}", version);
            Log.Information("Artifacts: {ArtifactsDirectory}", ArtifactsDirectory);
        });
}