namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Load;

public sealed class LoadRecipeArgs
{
	public string FilePath { get; }

	public LoadRecipeArgs(string filePath)
	{
		FilePath = filePath;
	}
}
