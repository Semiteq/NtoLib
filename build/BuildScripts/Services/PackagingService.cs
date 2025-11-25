using System;
using System.IO;
using System.IO.Compression;

using BuildScripts.Infrastructure;

using Nuke.Common.IO;

using Serilog;

namespace BuildScripts.Services;

public sealed class PackagingService
{
	private readonly BuildContext _ctx;

	public PackagingService(BuildContext ctx)
	{
		_ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
	}

	public AbsolutePath CreateReleaseArchive()
	{
		var version = _ctx.ReadAssemblyInformationalVersion();
		var archiveName = $"NtoLib_v{version}.zip";
		var archivePath = _ctx.ArtifactsDirectory / archiveName;

		Log.Information("Creating release archive: {Name}", archiveName);

		var tempDir = _ctx.TemporaryDirectory / "archive";
		tempDir.CreateOrCleanDirectory();

		var sourceDir = _ctx.TargetDirectory;

		var filesToArchive = new[] { "NtoLib.dll", "System.Resources.Extensions.dll" };
		var count = 0;

		foreach (var f in filesToArchive)
		{
			var src = sourceDir / f;
			if (!src.FileExists())
			{
				Log.Warning("File not found for archive: {File}", f);
				continue;
			}

			File.Copy(src, tempDir / f, overwrite: true);
			Log.Debug("Added: {FileName}", f);
			count++;
		}

		Log.Debug("Added {Count} files to archive", count);

		var yamlTemplatesSource = _ctx.NtoLibProject.Directory / "Recipes" / "MbeTable" / "YamlTemplates";
		if (yamlTemplatesSource.DirectoryExists())
		{
			Log.Debug("Adding YamlTemplates");
			CopyDirectoryRecursively(yamlTemplatesSource, tempDir / "YamlTemplates");
		}
		else
		{
			Log.Warning("YamlTemplates folder not found: {Path}", yamlTemplatesSource);
		}

		if (_ctx.NtoLibRegBat.FileExists())
		{
			File.Copy(_ctx.NtoLibRegBat, tempDir / "NtoLib_reg.bat", overwrite: true);
			Log.Debug("Added: NtoLib_reg.bat");
		}
		else
		{
			Log.Warning("NtoLib_reg.bat not found: {Path}", _ctx.NtoLibRegBat);
		}

		if (archivePath.FileExists())
			archivePath.DeleteFile();

		ZipFile.CreateFromDirectory(tempDir, archivePath);

		var archiveInfo = new FileInfo(archivePath);
		Log.Information("Archive created: {ArchiveName} ({Size} KB)", archivePath.Name, archiveInfo.Length / 1024);
		Log.Information("Location: {ArchivePath}", archivePath);

		return archivePath;
	}

	private static void CopyDirectoryRecursively(AbsolutePath source, AbsolutePath destination)
	{
		destination.CreateDirectory();

		foreach (var file in Directory.GetFiles(source))
		{
			var fileName = Path.GetFileName(file);
			File.Copy(file, destination / fileName, overwrite: true);
			Log.Debug("Copied file: {FileName}", fileName);
		}

		foreach (var dir in Directory.GetDirectories(source))
		{
			var dirName = Path.GetFileName(dir);
			CopyDirectoryRecursively(dir, destination / dirName);
		}
	}
}
