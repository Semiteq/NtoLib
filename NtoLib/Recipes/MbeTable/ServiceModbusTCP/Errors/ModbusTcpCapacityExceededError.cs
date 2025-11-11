using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpCapacityExceededError : BilingualError
{
    public string AreaType { get; }
    public int Required { get; }
    public int Available { get; }

    public ModbusTcpCapacityExceededError(string areaType, int required, int available)
        : base(
            $"PLC {areaType} area capacity exceeded: required {required}, available {available}",
            $"Превышена емкость области {areaType} контроллера: требуется {required}, доступно {available}")
    {
        AreaType = areaType;
        Required = required;
        Available = available;
    }
}