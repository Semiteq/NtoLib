using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

public sealed record ColumnDefinition(
	ColumnIdentifier Key,
	string Code,
	string UiName,
	string PropertyTypeId,
	string ColumnType,
	int MaxDropdownItems,
	int Width,
	int MinimalWidth,
	UiAlignment Alignment,
	PlcMapping? PlcMapping,
	bool ReadOnly,
	bool SaveToCsv);
