using System;

using FB;

using InSAT.OPC;

using MasterSCADA.Hlp;

using NtoLib.ConfigLoader.Entities;

namespace NtoLib.ConfigLoader.Pins;

public class PinGroupManager
{
	private readonly StaticFBBase _fb;

	private const int IdShutterInGroup = 1000;
	private const int IdShutterOutGroup = 2000;
	private const int IdSourcesInGroup = 3000;
	private const int IdSourcesOutGroup = 4000;
	private const int IdChamberHeaterInGroup = 5000;
	private const int IdChamberHeaterOutGroup = 6000;
	private const int IdWaterInGroup = 7000;
	private const int IdWaterOutGroup = 8000;

	private const int PinOffsetInsideGroup = 1;

	private int _firstShutterInPinId;
	private int _firstShutterOutPinId;
	private int _firstSourcesInPinId;
	private int _firstSourcesOutPinId;
	private int _firstChamberHeaterInPinId;
	private int _firstChamberHeaterOutPinId;
	private int _firstWaterInPinId;
	private int _firstWaterOutPinId;

	public PinGroupManager(StaticFBBase fb)
	{
		_fb = fb ?? throw new ArgumentNullException(nameof(fb));
	}

	public void CreateAllGroups(
		uint shutterQuantity,
		uint sourcesQuantity,
		uint chamberHeaterQuantity,
		uint waterQuantity)
	{
		CreatePinGroup(
			"Shutters_IN",
			"Shutters_OUT",
			IdShutterInGroup,
			IdShutterOutGroup,
			shutterQuantity,
			out _firstShutterInPinId,
			out _firstShutterOutPinId);

		CreatePinGroup(
			"Sources_IN",
			"Sources_OUT",
			IdSourcesInGroup,
			IdSourcesOutGroup,
			sourcesQuantity,
			out _firstSourcesInPinId,
			out _firstSourcesOutPinId);

		CreatePinGroup(
			"ChamberHeaters_IN",
			"ChamberHeaters_OUT",
			IdChamberHeaterInGroup,
			IdChamberHeaterOutGroup,
			chamberHeaterQuantity,
			out _firstChamberHeaterInPinId,
			out _firstChamberHeaterOutPinId);

		CreatePinGroup(
			"Water_IN",
			"Water_OUT",
			IdWaterInGroup,
			IdWaterOutGroup,
			waterQuantity,
			out _firstWaterInPinId,
			out _firstWaterOutPinId);
	}

	public LoaderDto ReadInputPins(
		uint shutterQuantity,
		uint sourcesQuantity,
		uint chamberHeaterQuantity,
		uint waterQuantity)
	{
		var shutters = ReadPinGroupValues(_firstShutterInPinId, shutterQuantity);
		var sources = ReadPinGroupValues(_firstSourcesInPinId, sourcesQuantity);
		var chamberHeaters = ReadPinGroupValues(_firstChamberHeaterInPinId, chamberHeaterQuantity);
		var waterChannels = ReadPinGroupValues(_firstWaterInPinId, waterQuantity);

		return new LoaderDto(shutters, sources, chamberHeaters, waterChannels);
	}

	public void WriteOutputPins(LoaderDto dto)
	{
		WritePinGroupValues(_firstShutterOutPinId, dto.Shutters);
		WritePinGroupValues(_firstSourcesOutPinId, dto.Sources);
		WritePinGroupValues(_firstChamberHeaterOutPinId, dto.ChamberHeaters);
		WritePinGroupValues(_firstWaterOutPinId, dto.WaterChannels);
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
		var inGroup = _fb.Root.AddGroup(inGroupId, inGroupName);
		var outGroup = _fb.Root.AddGroup(outGroupId, outGroupName);

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
		if (values == null || values.Length == 0)
		{
			return;
		}

		for (var offset = 0; offset < values.Length; offset++)
		{
			var pinId = firstPinId + offset;
			var value = values[offset] ?? string.Empty;
			_fb.SetPinValue(pinId, value);
		}
	}
}
