using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

public sealed class FbPinAccessor
{
	private readonly MbeTableFB _fb;
	public FbPinAccessor(MbeTableFB fb)
	{
		_fb = fb;
	}

	public OpcQuality GetQuality(int pinId)
	{
		return _fb.GetPinQuality(pinId);
	}

	public T GetValue<T>(int pinId)
	{
		return _fb.GetPinValue<T>(pinId);
	}
}
