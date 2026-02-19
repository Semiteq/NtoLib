using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Installer;

public sealed class InstallerService
{
	private static readonly string[] _requiredZipEntries =
	{
		"NtoLib.dll",
		"NtoLib_reg.bat"
	};

	private static readonly string[] _dllArtifacts =
	{
		"NtoLib.dll",
		"System.Resources.Extensions.dll",
		"NtoLib_reg.bat"
	};

	private const string DefaultConfigFolderName = "DefaultConfig";
	private const string ZoneIdentifierSuffix = ":Zone.Identifier";

	private readonly IProgress<InstallationProgress> _progress;
	private readonly InstallationPaths _paths;

	public InstallerService(InstallationPaths paths, IProgress<InstallationProgress> progress)
	{
		_paths = paths ?? throw new ArgumentNullException(nameof(paths));
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
	}

	public static string? FindZipArchive()
	{
		var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
		var zipFiles = Directory.GetFiles(exeDirectory, "NtoLib_v*.zip");

		if (zipFiles.Length == 0)
		{
			return null;
		}

		return zipFiles
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.First();
	}

	public static void ValidateZipArchive(string zipPath)
	{
		if (!File.Exists(zipPath))
		{
			throw new FileNotFoundException($"Archive not found: {zipPath}");
		}

		using var archive = ZipFile.OpenRead(zipPath);
		var entryNames = archive.Entries.Select(e => e.FullName).ToArray();

		foreach (var required in _requiredZipEntries)
		{
			if (!entryNames.Contains(required))
			{
				throw new InvalidOperationException(
					$"Archive is missing required entry: {required}");
			}
		}
	}

	public Task InstallAsync(string zipPath, bool backupExisting, CancellationToken cancellationToken)
	{
		return Task.Run(() => Install(zipPath, backupExisting, cancellationToken), cancellationToken);
	}

	private void Install(string zipPath, bool backupExisting, CancellationToken cancellationToken)
	{
		ReportProgress(0, "Starting installation...");

		ValidatePrerequisites();
		cancellationToken.ThrowIfCancellationRequested();
		ReportProgress(10, "Prerequisites validated.");

		RemoveZoneIdentifier(zipPath);
		ReportProgress(12, "Archive unblocked (Zone.Identifier removed).");

		if (backupExisting)
		{
			BackupExistingFiles();
			cancellationToken.ThrowIfCancellationRequested();
			ReportProgress(25, "Backup completed.");
		}
		else
		{
			ReportProgress(25, "Backup skipped by user.");
		}

		ExtractAndCopyArtifacts(zipPath);
		cancellationToken.ThrowIfCancellationRequested();
		ReportProgress(55, "Files copied successfully.");

		UnblockDestinationDlls();
		cancellationToken.ThrowIfCancellationRequested();
		ReportProgress(60, "Destination DLLs unblocked.");

		RunComRegistration();
		cancellationToken.ThrowIfCancellationRequested();
		ReportProgress(90, "COM registration completed.");

		ReportProgress(100, "Installation finished successfully.");
	}

	private void ValidatePrerequisites()
	{
		if (!Directory.Exists(_paths.DllDirectory))
		{
			throw new DirectoryNotFoundException(
				$"MasterSCADA directory not found: {_paths.DllDirectory}");
		}

		if (!File.Exists(_paths.NetregExePath))
		{
			throw new FileNotFoundException(
				$"netreg.exe not found: {_paths.NetregExePath}");
		}
	}

	private void BackupExistingFiles()
	{
		var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
		var backupDir = Path.Combine(_paths.BackupRootDirectory, timestamp);
		Directory.CreateDirectory(backupDir);

		ReportProgress(15, $"Backing up to {backupDir}");

		foreach (var artifact in _dllArtifacts)
		{
			var sourcePath = Path.Combine(_paths.DllDirectory, artifact);
			if (!File.Exists(sourcePath))
			{
				continue;
			}

			var destPath = Path.Combine(backupDir, artifact);
			File.Copy(sourcePath, destPath, overwrite: true);
			ReportProgress(18, $"  Backed up {artifact}");
		}

		if (!Directory.Exists(_paths.ConfigDirectory))
		{
			return;
		}

		var configBackupDir = Path.Combine(backupDir, DefaultConfigFolderName);
		CopyDirectory(_paths.ConfigDirectory, configBackupDir);
		ReportProgress(22, $"  Backed up {DefaultConfigFolderName}/");
	}

	private void ExtractAndCopyArtifacts(string zipPath)
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "NtoLib_Install_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);

		try
		{
			ReportProgress(30, "Extracting archive...");
			ZipFile.ExtractToDirectory(zipPath, tempDir);
			ReportProgress(40, "Archive extracted.");

			Directory.CreateDirectory(_paths.DllDirectory);

			foreach (var artifact in _dllArtifacts)
			{
				var sourcePath = Path.Combine(tempDir, artifact);
				if (!File.Exists(sourcePath))
				{
					continue;
				}

				var destPath = Path.Combine(_paths.DllDirectory, artifact);
				File.Copy(sourcePath, destPath, overwrite: true);
				ReportProgress(45, $"  Copied {artifact}");
			}

			var configSource = Path.Combine(tempDir, DefaultConfigFolderName);
			if (!Directory.Exists(configSource))
			{
				return;
			}

			if (Directory.Exists(_paths.ConfigDirectory))
			{
				Directory.Delete(_paths.ConfigDirectory, recursive: true);
			}

			CopyDirectory(configSource, _paths.ConfigDirectory);
			ReportProgress(55, $"  Copied {DefaultConfigFolderName}/");
		}
		finally
		{
			TryDeleteDirectory(tempDir);
		}
	}

	private void RunComRegistration()
	{
		ReportProgress(65, "Running COM registration (netreg.exe)...");

		var startInfo = new ProcessStartInfo
		{
			FileName = _paths.NetregExePath,
			Arguments = "NtoLib.dll /showerror",
			WorkingDirectory = _paths.DllDirectory,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		using var process = Process.Start(startInfo);
		if (process == null)
		{
			throw new InvalidOperationException("Failed to start netreg.exe.");
		}

		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();

		if (!string.IsNullOrWhiteSpace(stdout))
		{
			ReportProgress(80, $"  netreg stdout: {stdout.Trim()}");
		}

		if (!string.IsNullOrWhiteSpace(stderr))
		{
			ReportProgress(82, $"  netreg stderr: {stderr.Trim()}");
		}

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException(
				$"netreg.exe exited with code {process.ExitCode}. " +
				$"stdout: {stdout.Trim()} stderr: {stderr.Trim()}");
		}

		ReportProgress(85, "COM registration succeeded.");
	}

	private static void RemoveZoneIdentifier(string filePath)
	{
		var adsPath = filePath + ZoneIdentifierSuffix;

		try
		{
			File.Delete(adsPath);
		}
		catch (UnauthorizedAccessException)
		{
			// File may be read-only or access is denied — not fatal
		}
		catch (IOException)
		{
			// ADS may not exist or filesystem does not support ADS — not fatal
		}
	}

	private void UnblockDestinationDlls()
	{
		var dllFiles = Directory.GetFiles(_paths.DllDirectory, "*.dll");

		foreach (var dllFile in dllFiles)
		{
			RemoveZoneIdentifier(dllFile);
			ReportProgress(58, $"  Unblocked {Path.GetFileName(dllFile)}");
		}
	}

	private void ReportProgress(int percentage, string message)
	{
		_progress.Report(new InstallationProgress(percentage, message));
	}

	private static void CopyDirectory(string sourceDir, string destDir)
	{
		Directory.CreateDirectory(destDir);

		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var destFile = Path.Combine(destDir, Path.GetFileName(file));
			File.Copy(file, destFile, overwrite: true);
		}

		foreach (var subDir in Directory.GetDirectories(sourceDir))
		{
			var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
			CopyDirectory(subDir, destSubDir);
		}
	}

	private static void TryDeleteDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
		}
		catch
		{
			// Best-effort cleanup of temp directory
		}
	}
}
