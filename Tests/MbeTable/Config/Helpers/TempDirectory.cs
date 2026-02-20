namespace Tests.MbeTable.Config.Helpers;

public sealed class TempDirectory : IDisposable
{
	public TempDirectory(string? name = null)
	{
		var baseDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NtoLib.ConfigTests");
		Directory.CreateDirectory(baseDir);

		Path = System.IO.Path.Combine(baseDir, name ?? Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path);
	}

	public string Path { get; }

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(Path))
			{
				Directory.Delete(Path, recursive: true);
			}
		}
		catch
		{
			// Ignore
		}
	}
}
