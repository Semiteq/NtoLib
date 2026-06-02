using MasterSCADA.Hlp;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
{
	private const int IdHmiFloatBaseAddr = 1003;
	private const int IdHmiFloatAreaSize = 1004;
	private const int IdHmiIntBaseAddr = 1005;
	private const int IdHmiIntAreaSize = 1006;
	private const int IdHmiBoolBaseAddr = 1007;
	private const int IdHmiBoolAreaSize = 1008;
	private const int IdHmiControlBaseAddr = 1009;
	private const int IdHmiIp1 = 1010;
	private const int IdHmiIp2 = 1011;
	private const int IdHmiIp3 = 1012;
	private const int IdHmiIp4 = 1013;
	private const int IdHmiPort = 1014;

	protected override void CreatePinMap(bool newObject)
	{
		base.CreatePinMap(newObject);
		var state = EnsureConfigurationLoaded();
		CreatePinsFromConfiguration(state);
		FirePinSpaceChanged();
	}

	private void CreatePinsFromConfiguration(AppConfiguration state)
	{
		foreach (var pinGroup in state.PinGroupData)
		{
			var groupNode = Root.AddGroup(pinGroup.PinGroupId, pinGroup.GroupName);

			foreach (var (pinId, pinName) in RecipeFbConfigurationHelper.EnumerateGroupPins(pinGroup))
			{
				groupNode.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), "");
			}
		}
	}

	private void UpdateUiConnectionPins()
	{
		VisualPins.SetValue<uint>(IdHmiFloatBaseAddr, UFloatBaseAddr);
		VisualPins.SetValue<uint>(IdHmiFloatAreaSize, UFloatAreaSize);
		VisualPins.SetValue<uint>(IdHmiIntBaseAddr, UIntBaseAddr);
		VisualPins.SetValue<uint>(IdHmiIntAreaSize, UIntAreaSize);
		VisualPins.SetValue<uint>(IdHmiControlBaseAddr, UControlBaseAddr);
		VisualPins.SetValue<uint>(IdHmiIp1, UControllerIp1);
		VisualPins.SetValue<uint>(IdHmiIp2, UControllerIp2);
		VisualPins.SetValue<uint>(IdHmiIp3, UControllerIp3);
		VisualPins.SetValue<uint>(IdHmiIp4, UControllerIp4);
		VisualPins.SetValue<uint>(IdHmiPort, ControllerTcpPort);
	}
}
