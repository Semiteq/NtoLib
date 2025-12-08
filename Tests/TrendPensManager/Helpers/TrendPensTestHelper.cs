using System.Text.Json;

using FluentResults;

using NtoLib.TrendPensManager.Entities;

namespace Tests.TrendPensManager.Helpers;

public static class TrendPensTestHelper
{
	public static (List<ChannelInfo> Channels, List<string> Warnings) LoadChannelsFromJson(string trendJsonPath)
	{
		var json = File.ReadAllText(trendJsonPath);
		var dump = JsonSerializer.Deserialize<TrendTreeDump>(json) ??
				   throw new InvalidOperationException("Invalid trend dump json");

		var channels = new List<ChannelInfo>();
		var warnings = new List<string>();

		foreach (var service in dump.Services)
		{
			var serviceType = service.Name switch
			{
				"БКТ" => ServiceType.Heaters,
				"БП" => ServiceType.ChamberHeaters,
				"БУЗ" => ServiceType.Shutters,
				_ => ServiceType.Other
			};

			foreach (var ch in service.Channels)
			{
				if (!ch.HasUsedPin)
				{
					warnings.Add($"Channel {ch.FullName} has no 'Used' pin, skipped");
					continue;
				}

				var parameters = ch.Parameters
					.Select(p => new ParameterInfo(p.Name, p.FullPath))
					.ToList();

				channels.Add(new ChannelInfo(service.Name, serviceType, ch.ChannelNumber, ch.Used, parameters));
			}
		}

		return (channels, warnings);
	}

	public static Result<Dictionary<ServiceType, string[]>> LoadConfigFromJson(string configJsonPath)
	{
		var json = File.ReadAllText(configJsonPath);
		var dump = JsonSerializer.Deserialize<ConfigLoaderDump>(json) ??
				   throw new InvalidOperationException("Invalid config loader dump json");

		var dict = new Dictionary<ServiceType, string[]>();

		if (dump.Groups.TryGetValue("Sources_OUT", out var sources))
		{
			dict[ServiceType.Heaters] = sources;
		}

		if (dump.Groups.TryGetValue("ChamberHeaters_OUT", out var chambers))
		{
			dict[ServiceType.ChamberHeaters] = chambers;
		}

		if (dump.Groups.TryGetValue("Shutters_OUT", out var shutters))
		{
			dict[ServiceType.Shutters] = shutters;
		}

		return Result.Ok(dict);
	}

	public class TrendTreeDump
	{
		public string TrendRootPath { get; set; } = string.Empty;
		public List<ServiceDump> Services { get; set; } = new();
	}

	public class ServiceDump
	{
		public string Name { get; set; } = string.Empty;
		public List<ChannelDump> Channels { get; set; } = new();
	}

	public class ChannelDump
	{
		public string Name { get; set; } = string.Empty;
		public int ChannelNumber { get; set; }
		public bool HasUsedPin { get; set; }
		public bool Used { get; set; }
		public string FullName { get; set; } = string.Empty;
		public List<ParameterDump> Parameters { get; set; } = new();
	}

	public class ParameterDump
	{
		public string Name { get; set; } = string.Empty;
		public string FullPath { get; set; } = string.Empty;
	}

	public class ConfigLoaderDump
	{
		public string ConfigLoaderPath { get; set; } = string.Empty;
		public Dictionary<string, string[]> Groups { get; set; } = new();
	}
}
