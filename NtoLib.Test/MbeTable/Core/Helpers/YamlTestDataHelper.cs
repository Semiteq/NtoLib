using System;
using System.IO;

namespace NtoLib.Test.MbeTable.Core.Helpers;

public static class YamlTestDataHelper
{
	public static string PrepareYamlConfigDirectory()
	{
		var testDataRoot = GetYamlConfigRoot();
		var tempDir = Path.Combine(Path.GetTempPath(), "NtoLib.FacadeTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);

		CopyDirectory(testDataRoot, tempDir);
		return tempDir;
	}

	private static string GetYamlConfigRoot()
	{
		var dir = AppContext.BaseDirectory;
		for (var i = 0; i < 12 && !string.IsNullOrEmpty(dir); i++)
		{
			var probe = Path.Combine(dir, "MbeTable", "YamlConfigs");
			if (Directory.Exists(probe))
				return probe;

			dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
		}

		throw new DirectoryNotFoundException(
			"YamlConfigs root not found. Expected 'MbeTable/YamlConfigs' up the directory tree.");
	}

	private static void CopyDirectory(string sourceDir, string destDir)
	{
		foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
		{
			var rel = ComputeRelativePath(sourceDir, dirPath);
			var target = Path.Combine(destDir, rel);
			Directory.CreateDirectory(target);
		}

		foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
		{
			var rel = ComputeRelativePath(sourceDir, filePath);
			var target = Path.Combine(destDir, rel);
			var targetDir = Path.GetDirectoryName(target);
			if (!string.IsNullOrEmpty(targetDir))
				Directory.CreateDirectory(targetDir);

			File.Copy(filePath, target, overwrite: true);
		}
	}

	private static string ComputeRelativePath(string baseDir, string fullPath)
	{
		var normBase = baseDir.EndsWith(Path.DirectorySeparatorChar.ToString())
			? baseDir
			: baseDir + Path.DirectorySeparatorChar;

		if (fullPath.StartsWith(normBase, StringComparison.OrdinalIgnoreCase))
			return fullPath.Substring(normBase.Length);

		return Path.GetFileName(fullPath) ?? string.Empty;
	}
}
