using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.PinGroups;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Mapping;

/// <summary>
/// Maps YamlPinGroupDefinition to PinGroupData.
/// </summary>
public sealed class PinGroupDataMapper : IEntityMapper<YamlPinGroupDefinition, PinGroupData>
{
	public PinGroupData Map(YamlPinGroupDefinition source)
	{
		return new PinGroupData(
			source.GroupName,
			source.PinGroupId,
			source.FirstPinId,
			source.PinQuantity);
	}

	public IReadOnlyList<PinGroupData> MapMany(IEnumerable<YamlPinGroupDefinition> sources)
	{
		return sources.Select(Map).ToList();
	}
}
