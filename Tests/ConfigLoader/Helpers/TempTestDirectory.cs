namespace Tests.ConfigLoader.Helpers;

public sealed class TempTestDirectory : IDisposable
{
	public string Path { get; }

	public TempTestDirectory()
	{
		Path = System.IO.Path.Combine(
			System.IO.Path.GetTempPath(),
			"NtoLib. ConfigLoaderTests",
			Guid.NewGuid().ToString("N"));

		Directory.CreateDirectory(Path);
	}

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
			// Ignore cleanup errors in tests
		}
	}
}
