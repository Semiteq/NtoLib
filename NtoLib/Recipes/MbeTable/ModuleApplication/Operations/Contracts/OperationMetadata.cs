namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

/// <summary>
/// Declarative description of an operation: display name, completion style,
/// concurrency flag, and post-success effects. Instances are static singletons
/// keyed by <see cref="OperationId"/>.
/// </summary>
public sealed class OperationMetadata
{
	public OperationId Id { get; }
	public OperationKind Kind { get; }
	public string DisplayNameRu { get; }
	public CompletionMessageKind CompletionMessage { get; }
	public bool IsLongRunning { get; }
	public bool UpdatesPolicyReasons { get; }
	public ConsistencyEffect ConsistencyEffect { get; }

	private OperationMetadata(
		OperationId id,
		OperationKind kind,
		string displayNameRu,
		CompletionMessageKind completionMessage,
		bool isLongRunning,
		bool updatesPolicyReasons,
		ConsistencyEffect consistencyEffect)
	{
		Id = id;
		Kind = kind;
		DisplayNameRu = displayNameRu;
		CompletionMessage = completionMessage;
		IsLongRunning = isLongRunning;
		UpdatesPolicyReasons = updatesPolicyReasons;
		ConsistencyEffect = consistencyEffect;
	}

	public static readonly OperationMetadata Load = new(
		OperationId.Load, OperationKind.Loading,
		"загрузка рецепта", CompletionMessageKind.Success,
		isLongRunning: true, updatesPolicyReasons: true,
		ConsistencyEffect.MarkInconsistent);

	public static readonly OperationMetadata Save = new(
		OperationId.Save, OperationKind.Saving,
		"сохранение рецепта", CompletionMessageKind.Success,
		isLongRunning: true, updatesPolicyReasons: false,
		ConsistencyEffect.None);

	public static readonly OperationMetadata Send = new(
		OperationId.Send, OperationKind.Transferring,
		"отправка рецепта", CompletionMessageKind.Success,
		isLongRunning: true, updatesPolicyReasons: false,
		ConsistencyEffect.MarkConsistent);

	public static readonly OperationMetadata Receive = new(
		OperationId.Receive, OperationKind.Transferring,
		"чтение рецепта", CompletionMessageKind.Success,
		isLongRunning: true, updatesPolicyReasons: true,
		ConsistencyEffect.MarkConsistent);

	public static readonly OperationMetadata AddStep = new(
		OperationId.AddStep, OperationKind.Other,
		"добавление строки", CompletionMessageKind.Info,
		isLongRunning: false, updatesPolicyReasons: true,
		ConsistencyEffect.MarkInconsistent);

	public static readonly OperationMetadata RemoveStep = new(
		OperationId.RemoveStep, OperationKind.Other,
		"удаление строки", CompletionMessageKind.Info,
		isLongRunning: false, updatesPolicyReasons: true,
		ConsistencyEffect.MarkInconsistent);

	public static readonly OperationMetadata EditCell = new(
		OperationId.EditCell, OperationKind.Other,
		"обновление ячейки", CompletionMessageKind.None,
		isLongRunning: false, updatesPolicyReasons: true,
		ConsistencyEffect.MarkInconsistent);

	public static readonly OperationMetadata CopyRows = new(
		OperationId.CopyRows, OperationKind.Other,
		"копирование строк", CompletionMessageKind.Info,
		isLongRunning: false, updatesPolicyReasons: false,
		ConsistencyEffect.None);

	public static readonly OperationMetadata CutRows = new(
		OperationId.CutRows, OperationKind.Other,
		"вырезание строк", CompletionMessageKind.Info,
		isLongRunning: false, updatesPolicyReasons: true,
		ConsistencyEffect.MarkInconsistent);

	public static readonly OperationMetadata PasteRows = new(
		OperationId.PasteRows, OperationKind.Other,
		"вставка строк", CompletionMessageKind.Info,
		isLongRunning: false, updatesPolicyReasons: true,
		ConsistencyEffect.MarkInconsistent);

	public static readonly OperationMetadata DeleteRows = new(
		OperationId.DeleteRows, OperationKind.Other,
		"удаление нескольких строк", CompletionMessageKind.Info,
		isLongRunning: false, updatesPolicyReasons: true,
		ConsistencyEffect.MarkInconsistent);
}
