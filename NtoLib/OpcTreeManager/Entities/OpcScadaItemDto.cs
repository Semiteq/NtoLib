using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using OpcUaClient.Client.Common.Data;

namespace NtoLib.OpcTreeManager.Entities;

// Mutable init-setters required: System.Text.Json deserialiser on .NET 4.8 does not support positional record constructors without a custom converter.
public sealed class OpcScadaItemDto
{
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	[JsonPropertyName("id")]
	public int Id { get; init; }

	[JsonPropertyName("nodeId")]
	public string NodeId { get; init; } = string.Empty;

	[JsonPropertyName("isNode")]
	public bool IsNode { get; init; }

	[JsonPropertyName("pinValueType")]
	public string PinValueType { get; init; } = string.Empty;

	[JsonPropertyName("dataType")]
	public string? DataType { get; init; }

	[JsonPropertyName("isArray")]
	public bool IsArray { get; init; }

	[JsonPropertyName("arrayCount")]
	public int ArrayCount { get; init; }

	[JsonPropertyName("deadbandType")]
	public string DeadbandType { get; init; } = string.Empty;

	[JsonPropertyName("deadBandValue")]
	public double DeadBandValue { get; init; }

	[JsonPropertyName("isOwnSubscriptionSettings")]
	public bool IsOwnSubscriptionSettings { get; init; }

	[JsonPropertyName("readPeriod")]
	public int ReadPeriod { get; init; }

	[JsonPropertyName("readTimeout")]
	public int ReadTimeout { get; init; }

	[JsonPropertyName("items")]
	public List<OpcScadaItemDto> Items { get; init; } = new();

	public static OpcScadaItemDto FromScadaItem(OpcUaScadaItem item)
	{
		return new OpcScadaItemDto
		{
			Name = item.Name,
			Id = item.Id,
			NodeId = item.NodeId,
			IsNode = item.IsNode,
			PinValueType = item.PinValueType.ToString(),
			DataType = item.DataType?.AssemblyQualifiedName,
			IsArray = item.IsArray,
			ArrayCount = item.ArrayCount,
			DeadbandType = item.DeadbandType.ToString(),
			DeadBandValue = item.DeadBandValue,
			IsOwnSubscriptionSettings = item.IsOwnSubscriptionSettings,
			ReadPeriod = item.ReadPeriod,
			ReadTimeout = item.ReadTimeout,
			Items = item.Items.Select(FromScadaItem).ToList(),
		};
	}

	public OpcUaScadaItem ToScadaItem()
	{
		if (!Enum.TryParse(PinValueType, out PinType pinType))
		{
			throw new InvalidOperationException(
				$"Cannot convert snapshot value '{PinValueType}' to {nameof(PinType)} for item '{Name}'.");
		}

		if (!Enum.TryParse(DeadbandType, out Opc.Ua.DeadbandType deadbandType))
		{
			throw new InvalidOperationException(
				$"Cannot convert snapshot value '{DeadbandType}' to {nameof(Opc.Ua.DeadbandType)} for item '{Name}'.");
		}

		var scadaItem = new OpcUaScadaItem
		{
			Name = Name,
			Id = Id,
			NodeId = NodeId,
			IsNode = IsNode,
			PinValueType = pinType,
			DataType = DataType != null ? Type.GetType(DataType) : null,
			IsArray = IsArray,
			ArrayCount = ArrayCount,
			DeadbandType = deadbandType,
			DeadBandValue = DeadBandValue,
			IsOwnSubscriptionSettings = IsOwnSubscriptionSettings,
			ReadPeriod = ReadPeriod,
			ReadTimeout = ReadTimeout,
		};

		foreach (var child in Items)
		{
			scadaItem.Items.Add(child.ToScadaItem());
		}

		return scadaItem;
	}
}
