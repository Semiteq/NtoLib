using NtoLib.ConfigLoader.Entities;
using NtoLib.ConfigLoader.Facade;

namespace Tests.ConfigLoader.Helpers;

public static class ConfigLoaderTestHelper
{
	private const string ValidSubfolder = "Valid";
	private const string InvalidSubfolder = "Invalid";
	private const string ConfigFileName = "NamesConfig.yaml";

	private static readonly object _fileLock = new();

	public static IConfigLoaderService CreateService()
	{
		return new ConfigLoaderService(_fileLock, ConfigLoaderGroups.Default);
	}

	public static (IConfigLoaderService Service, TempTestDirectory TempDir) PrepareValidCase(string caseName)
	{
		var tempDir = CopyTestCase(ValidSubfolder, caseName);
		var service = CreateService();

		return (service, tempDir);
	}

	public static (IConfigLoaderService Service, TempTestDirectory TempDir) PrepareInvalidCase(string caseName)
	{
		var tempDir = CopyTestCase(InvalidSubfolder, caseName);
		var service = CreateService();

		return (service, tempDir);
	}

	public static string GetConfigFilePath(TempTestDirectory tempDir)
	{
		return Path.Combine(tempDir.Path, ConfigFileName);
	}

	public static TempTestDirectory CreateEmptyTempDirectory()
	{
		return new TempTestDirectory();
	}

	private static TempTestDirectory CopyTestCase(string subfolder, string caseName)
	{
		var testDataRoot = GetTestDataRoot();
		var sourceDir = Path.Combine(testDataRoot, subfolder, caseName);

		if (!Directory.Exists(sourceDir))
		{
			throw new DirectoryNotFoundException($"Test case not found: {sourceDir}");
		}

		var tempDir = new TempTestDirectory();
		CopyDirectory(sourceDir, tempDir.Path);

		return tempDir;
	}

	private static string GetTestDataRoot()
	{
		var dir = AppContext.BaseDirectory;

		for (var i = 0; i < 12 && !string.IsNullOrEmpty(dir); i++)
		{
			var probe = Path.Combine(dir, "ConfigLoader", "TestData");
			if (Directory.Exists(probe))
			{
				return probe;
			}

			dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
		}

		throw new DirectoryNotFoundException(
			"TestData root not found. Expected 'ConfigLoader/TestData' up the directory tree.");
	}

	private static void CopyDirectory(string sourceDir, string destDir)
	{
		foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
		{
			var rel = GetRelativePath(sourceDir, dirPath);
			var target = Path.Combine(destDir, rel);
			Directory.CreateDirectory(target);
		}

		foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
		{
			var rel = GetRelativePath(sourceDir, filePath);
			var target = Path.Combine(destDir, rel);
			var targetDir = Path.GetDirectoryName(target);

			if (!string.IsNullOrEmpty(targetDir))
			{
				Directory.CreateDirectory(targetDir);
			}

			File.Copy(filePath, target, overwrite: true);
		}
	}

	private static string GetRelativePath(string baseDir, string fullPath)
	{
		var normBase = baseDir.EndsWith(Path.DirectorySeparatorChar.ToString())
			? baseDir
			: baseDir + Path.DirectorySeparatorChar;

		if (fullPath.StartsWith(normBase, StringComparison.OrdinalIgnoreCase))
		{
			return fullPath.Substring(normBase.Length);
		}

		return Path.GetFileName(fullPath) ?? string.Empty;
	}
}
