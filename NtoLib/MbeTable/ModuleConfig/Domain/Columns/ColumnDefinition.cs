using NtoLib.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.MbeTable.ModuleConfig.Domain.Columns;

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
