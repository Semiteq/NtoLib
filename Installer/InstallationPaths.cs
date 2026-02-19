using System.IO;

namespace Installer;

public sealed class InstallationPaths
{
	public const string DefaultDllDirectory = @"C:\Program Files (x86)\MPSSoft\MasterSCADA";
	public const string DefaultConfigDirectory = @"C:\DISTR\Config";
	public const string DefaultBackupRootDirectory = @"C:\DISTR\Backup\NtoLib";
	public const string NetregExeName = "netreg.exe";

	public string DllDirectory { get; set; } = DefaultDllDirectory;
	public string ConfigDirectory { get; set; } = DefaultConfigDirectory;
	public string BackupRootDirectory { get; set; } = DefaultBackupRootDirectory;

	public string NetregExePath => Path.Combine(DllDirectory, NetregExeName);
}
