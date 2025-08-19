namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public class CommunicationSettingsProvider : ICommunicationSettingsProvider
{
    private readonly MbeTableFB _fb;

    public CommunicationSettingsProvider(MbeTableFB fb)
    {
        _fb = fb;
    }

    public CommunicationSettings GetSettings()
    {
        return new CommunicationSettings
        {
            IsRecipeActive = _fb.IsRecipeActive(),
            LineNumber = _fb.GetLineNumber(),
            
            FloatBaseAddr = (int)_fb.UFloatBaseAddr,
            IntBaseAddr = (int)_fb.UIntBaseAddr,
            BoolBaseAddr = (int)_fb.UBoolBaseAddr,
            
            ControlBaseAddr = (int)_fb.UControlBaseAddr,
            
            FloatAreaSize = (int)_fb.UFloatAreaSize,
            IntAreaSize = (int)_fb.UIntAreaSize,
            BoolAreaSize = (int)_fb.UBoolAreaSize,
            
            Ip1 = (int)_fb.ControllerIp1,
            Ip2 = (int)_fb.ControllerIp2,
            Ip3 = (int)_fb.ControllerIp3,
            Ip4 = (int)_fb.ControllerIp4,
            
            Port = (int)_fb.ControllerTcpPort,
        };
        
    }
}