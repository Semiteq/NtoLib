using System;

using FB;

using InSAT.OPC;

using MasterSCADA.Hlp;

using NtoLib.ConfigLoader.Entities;

namespace NtoLib.ConfigLoader.Pins;

public class PinGroupManager
{
	private readonly StaticFBBase _fb;
	private readonly ConfigLoaderGroups _groups;

	private const int PinOffsetInsideGroup = 1;

	private int _firstShutterInPinId;
	private int _firstShutterOutPinId;
	private int _firstSourcesInPinId;
	private int _firstSourcesOutPinId;
	private int _firstChamberHeaterInPinId;
	private int _firstChamberHeaterOutPinId;
	private int _firstWaterInPinId;
	private int _firstWaterOutPinId;
	private int _firstGasesInPinId;
	private int _firstGasesOutPinId;

	public PinGroupManager(StaticFBBase fb, ConfigLoaderGroups groups)
	{
		_fb = fb ?? throw new ArgumentNullException(nameof(fb));
		_groups = groups ?? throw new ArgumentNullException(nameof(groups));
	}

	public void CreateAllGroups()
	{
		CreatePinGroup(
			_groups.Shutters.Name,
			_groups.Shutters.YamlSection,
			_groups.Shutters.InBaseId,
			_groups.Shutters.OutBaseId,
			_groups.Shutters.Capacity,
			out _firstShutterInPinId,
			out _firstShutterOutPinId);

		CreatePinGroup(
			_groups.Sources.Name,
			_groups.Sources.YamlSection,
			_groups.Sources.InBaseId,
			_groups.Sources.OutBaseId,
			_groups.Sources.Capacity,
			out _firstSourcesInPinId,
			out _firstSourcesOutPinId);

		CreatePinGroup(
			_groups.ChamberHeaters.Name,
			_groups.ChamberHeaters.YamlSection,
			_groups.ChamberHeaters.InBaseId,
			_groups.ChamberHeaters.OutBaseId,
			_groups.ChamberHeaters.Capacity,
			out _firstChamberHeaterInPinId,
			out _firstChamberHeaterOutPinId);

		CreatePinGroup(
			_groups.Water.Name,
			_groups.Water.YamlSection,
			_groups.Water.InBaseId,
			_groups.Water.OutBaseId,
			_groups.Water.Capacity,
			out _firstWaterInPinId,
			out _firstWaterOutPinId);

		CreatePinGroup(
			_groups.Gases.Name,
			_groups.Gases.YamlSection,
			_groups.Gases.InBaseId,
			_groups.Gases.OutBaseId,
			_groups.Gases.Capacity,
			out _firstGasesInPinId,
			out _firstGasesOutPinId);
	}

	public LoaderDto ReadInputPins()
	{
		var shutters = ReadPinGroupValues(_firstShutterInPinId, _groups.Shutters.Capacity);
		var sources = ReadPinGroupValues(_firstSourcesInPinId, _groups.Sources.Capacity);
		var chamberHeaters = ReadPinGroupValues(_firstChamberHeaterInPinId, _groups.ChamberHeaters.Capacity);
		var waters = ReadPinGroupValues(_firstWaterInPinId, _groups.Water.Capacity);
		var gases = ReadPinGroupValues(_firstGasesInPinId, _groups.Gases.Capacity);

		return new LoaderDto(shutters, sources, chamberHeaters, waters, gases);
	}

	public void WriteOutputPins(LoaderDto dto)
	{
		WritePinGroupValues(_firstShutterOutPinId, dto.Shutters);
		WritePinGroupValues(_firstSourcesOutPinId, dto.Sources);
		WritePinGroupValues(_firstChamberHeaterOutPinId, dto.ChamberHeaters);
		WritePinGroupValues(_firstWaterOutPinId, dto.WaterChannels);
		WritePinGroupValues(_firstGasesOutPinId, dto.Gases);
	}

	private void CreatePinGroup(
		string inGroupName,
		string outGroupName,
		int inGroupId,
		int outGroupId,
		uint quantity,
		out int firstInPinId,
		out int firstOutPinId)
	{
		var inGroup = _fb.Root.AddGroup(inGroupId, inGroupName + "_IN");
		var outGroup = _fb.Root.AddGroup(outGroupId, outGroupName + "_OUT");

		firstInPinId = inGroupId + PinOffsetInsideGroup;
		firstOutPinId = outGroupId + PinOffsetInsideGroup;

		for (var index = 0; index < quantity; index++)
		{
			var displayIndex = index + 1;
			var inPinId = firstInPinId + index;
			var outPinId = firstOutPinId + index;
			var pinName = displayIndex.ToString();

			inGroup.AddPinWithID(inPinId, pinName, PinType.Pin, typeof(string), string.Empty);
			outGroup.AddPinWithID(outPinId, pinName, PinType.Pout, typeof(string), string.Empty);
		}
	}

	private string[] ReadPinGroupValues(int firstPinId, uint quantity)
	{
		if (quantity == 0)
		{
			return Array.Empty<string>();
		}

		var result = new string[quantity];

		for (var offset = 0; offset < quantity; offset++)
		{
			var pinId = firstPinId + offset;
			var quality = _fb.GetPinQuality(pinId);

			if (quality != OpcQuality.Good)
			{
				result[offset] = string.Empty;
				continue;
			}

			var value = _fb.GetPinValue<string>(pinId);
			result[offset] = value ?? string.Empty;
		}

		return result;
	}

	private void WritePinGroupValues(int firstPinId, string[] values)
	{
		if (values.Length == 0)
		{
			return;
		}

		for (var offset = 0; offset < values.Length; offset++)
		{
			var pinId = firstPinId + offset;
			var value = values[offset];
			_fb.SetPinValue(pinId, value);
		}
	}
}
