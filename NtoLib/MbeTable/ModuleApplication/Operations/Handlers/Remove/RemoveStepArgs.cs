namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Remove;

public sealed class RemoveStepArgs
{
	public int Index { get; }

	public RemoveStepArgs(int index)
	{
		Index = index;
	}
}
