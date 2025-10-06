using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

internal sealed class FbPinAccessor : IPinAccessor
{
    private readonly MbeTableFB _fb;
    public FbPinAccessor(MbeTableFB fb) => _fb = fb;

    public OpcQuality GetQuality(int pinId) => _fb.GetPinQuality(pinId);
    public T GetValue<T>(int pinId) => _fb.GetPinValue<T>(pinId);
}