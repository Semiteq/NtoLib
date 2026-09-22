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

	/// <summary>
	/// Builds the live <see cref="OpcUaScadaItem"/> tree from this DTO, reusing each item's
	/// snapshot <c>Id</c> verbatim.
	/// </summary>
	public OpcUaScadaItem ToScadaItem()
	{
		var scadaItem = BuildWithoutChildren();

		foreach (var child in Items)
		{
			scadaItem.Items.Add(child.ToScadaItem());
		}

		return scadaItem;
	}

	/// <summary>
	/// <see cref="ToScadaItem"/> for a null or leaf <paramref name="spec"/>; otherwise keeps only the
	/// children the spec names and applies the same rule to each. Throws when a named child is absent
	/// from <see cref="Items"/>.
	/// </summary>
	public OpcUaScadaItem ToScadaItemPruned(NodeSpec? spec)
	{
		if (spec == null || spec.Children == null)
		{
			return ToScadaItem();
		}

		var scadaItem = BuildWithoutChildren();

		foreach (var childSpec in spec.Children)
		{
			var childDto = Items.FirstOrDefault(i => i.Name == childSpec.Name)
				?? throw new InvalidOperationException(
					$"Child '{childSpec.Name}' listed in desired spec is not present in snapshot item '{Name}'.");
			scadaItem.Items.Add(childDto.ToScadaItemPruned(childSpec));
		}

		return scadaItem;
	}

	private OpcUaScadaItem BuildWithoutChildren()
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

		Type? dataType = null;
		if (DataType != null)
		{
			dataType = Type.GetType(DataType);
			if (dataType == null)
			{
				throw new InvalidOperationException(
					$"Cannot resolve snapshot DataType '{DataType}' for item '{Name}'.");
			}
		}

		return new OpcUaScadaItem
		{
			Name = Name,
			Id = Id,
			NodeId = NodeId,
			IsNode = IsNode,
			PinValueType = pinType,
			DataType = dataType,
			IsArray = IsArray,
			ArrayCount = ArrayCount,
			DeadbandType = deadbandType,
			DeadBandValue = DeadBandValue,
			IsOwnSubscriptionSettings = IsOwnSubscriptionSettings,
			ReadPeriod = ReadPeriod,
			ReadTimeout = ReadTimeout,
		};
	}
}
