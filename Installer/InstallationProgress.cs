namespace Installer;

public sealed class InstallationProgress
{
	public InstallationProgress(int percentage, string message)
	{
		Percentage = percentage;
		Message = message;
	}

	public int Percentage { get; }
	public string Message { get; }
}
