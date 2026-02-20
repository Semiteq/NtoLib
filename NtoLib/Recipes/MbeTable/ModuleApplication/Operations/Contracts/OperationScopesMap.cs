using NtoLib.Recipes.MbeTable.ModuleApplication.Policy.Registry;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

public static class OperationScopesMap
{
	public static BlockingScope Map(OperationId operation)
	{
		return operation switch
		{
			OperationId.Save => BlockingScope.Save,
			OperationId.Send => BlockingScope.Send,
			OperationId.Load => BlockingScope.Load,
			OperationId.Receive => BlockingScope.Load,
			OperationId.AddStep => BlockingScope.Edit,
			OperationId.RemoveStep => BlockingScope.Edit,
			OperationId.EditCell => BlockingScope.Edit,
			OperationId.CopyRows => BlockingScope.None,
			OperationId.CutRows => BlockingScope.Edit,
			OperationId.PasteRows => BlockingScope.Edit,
			OperationId.DeleteRows => BlockingScope.Edit,
			OperationId.InsertRows => BlockingScope.Edit,
			_ => BlockingScope.None
		};
	}
}
