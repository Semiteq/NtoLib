namespace NtoLib.Test.MbeTable.Config.Helpers;

public static class TestDataCopier
{
    private const string ValidSubfolder = "Valid";
    private const string InvalidSubfolder = "Invalid";
    private const string BaselineFolderName = "Baseline";

    public static TempDirectory PrepareValidCase(string caseRelativePath)
    {
        if (string.IsNullOrWhiteSpace(caseRelativePath))
            throw new ArgumentNullException(nameof(caseRelativePath));

        var testDataRoot = GetTestDataRoot();
        var sourceDir = Path.Combine(testDataRoot, ValidSubfolder, caseRelativePath);
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Valid case not found: {sourceDir}");

        var tempDir = new TempDirectory();
        CopyDirectory(sourceDir, tempDir.Path);
        return tempDir;
    }

    public static TempDirectory PrepareInvalidCase(string caseNameUnderInvalid)
    {
        if (string.IsNullOrWhiteSpace(caseNameUnderInvalid))
            throw new ArgumentNullException(nameof(caseNameUnderInvalid));

        var testDataRoot = GetTestDataRoot();
        var baselineDir = Path.Combine(testDataRoot, ValidSubfolder, BaselineFolderName);
        if (!Directory.Exists(baselineDir))
            throw new DirectoryNotFoundException($"Baseline not found: {baselineDir}");

        var tempDir = new TempDirectory();
        CopyDirectory(baselineDir, tempDir.Path);

        var invalidDir = Path.Combine(testDataRoot, InvalidSubfolder, caseNameUnderInvalid);
        if (!Directory.Exists(invalidDir))
            throw new DirectoryNotFoundException($"Invalid case not found: {invalidDir}");

        CopyDirectory(invalidDir, tempDir.Path);
        return tempDir;
    }

    private static string GetTestDataRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 12 && !string.IsNullOrEmpty(dir); i++)
        {
            var probe1 = Path.Combine(dir, "MbeTable", "Config", "TestData");
            if (Directory.Exists(probe1))
                return probe1;

            var probe2 = Path.Combine(dir, "Config", "TestData");
            if (Directory.Exists(probe2))
                return probe2;

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new DirectoryNotFoundException(
            "TestData root not found. Expected 'MbeTable/Config/TestData' or 'Config/TestData' up the tree.");
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