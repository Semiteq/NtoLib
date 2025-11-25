using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Warnings;

public sealed class ModbusTcpZeroRowsWarning : BilingualWarning
{
	public ModbusTcpZeroRowsWarning()
		: base(
			"Recipe in PLC is empty (zero rows)",
			"Рецепт в контроллере пуст (нет строк)")
	{
	}
}
