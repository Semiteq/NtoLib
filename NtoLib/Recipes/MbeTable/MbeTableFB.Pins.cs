using System.Collections.Generic;

using InSAT.OPC;

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

	/// <summary>
	/// Creates pin map from configuration file definitions.
	/// </summary>
	/// <param name="newObject">True if creating pins for new object.</param>
	protected override void CreatePinMap(bool newObject)
	{
		base.CreatePinMap(newObject);
		var state = EnsureConfigurationLoaded();
		CreatePinsFromConfiguration(state);
		FirePinSpaceChanged();
	}

	/// <summary>
	/// Creates pins based on pin group definitions from configuration.
	/// </summary>
	/// <param name="state">Configuration state containing pin group definitions.</param>
	private void CreatePinsFromConfiguration(AppConfiguration state)
	{
		foreach (var pinGroup in state.PinGroupData)
		{
			var groupNode = Root.AddGroup(pinGroup.PinGroupId, pinGroup.GroupName);

			for (var i = 0; i < pinGroup.PinQuantity; i++)
			{
				var pinId = pinGroup.FirstPinId + i;
				var pinName = $"{pinGroup.GroupName}{i + 1}";
				groupNode.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), "");
			}
		}
	}

	/// <summary>
	/// Reads values from a group of pins.
	/// </summary>
	/// <param name="firstId">ID of the first pin in the group.</param>
	/// <param name="quantity">Number of pins in the group.</param>
	/// <returns>Dictionary mapping pin indices (1-based) to their string values.</returns>
	private Dictionary<int, string> ReadPinGroup(int firstId, int quantity)
	{
		var initialPinOffset = 1;
		var pinGroup = new Dictionary<int, string>(quantity + initialPinOffset);

		for (var offset = 0; offset < quantity; offset++)
		{
			var pinId = firstId + offset;
			if (GetPinQuality(pinId) != OpcQuality.Good)
			{
				continue;
			}

			var value = GetPinValue<string>(pinId);
			pinGroup[offset + initialPinOffset] = value;
		}

		return pinGroup;
	}

	/// <summary>
	/// Updates UI connection parameter pins with current values.
	/// </summary>
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
