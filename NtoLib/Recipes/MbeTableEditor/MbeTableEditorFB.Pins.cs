using MasterSCADA.Hlp;

using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;

namespace NtoLib.Recipes.MbeTableEditor;

public partial class MbeTableEditorFB
{
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
}
