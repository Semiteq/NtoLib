namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Save;

public sealed class SaveRecipeArgs
{
	public string FilePath { get; }

	public SaveRecipeArgs(string filePath)
	{
		FilePath = filePath;
	}
}
