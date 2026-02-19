namespace Installer;

public sealed class InstallationProgress
{
	public int Percentage { get; }
	public string Message { get; }

	public InstallationProgress(int percentage, string message)
	{
		Percentage = percentage;
		Message = message;
	}
}
