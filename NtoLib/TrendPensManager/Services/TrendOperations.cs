using System;
using System.Linq;

using FluentResults;

using MasterSCADA;
using MasterSCADA.Common;
using MasterSCADA.Graph.Objects;
using MasterSCADA.Graph.Styles;
using MasterSCADA.Hlp;
using MasterSCADA.Trend.Controls;
using MasterSCADA.Trend.Helpers;
using MasterSCADA.Trend.Services;

using MasterSCADALib;

using Microsoft.Extensions.Logging;

namespace NtoLib.TrendPensManager.Services;

public class TrendOperations
{
	private readonly IProjectHlp _project;
	private readonly ILogger<TrendOperations>? _logger;

	public TrendOperations(IProjectHlp project, ILogger<TrendOperations>? logger = null)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_logger = logger;
	}

	public Result AddPenToTrend(Trend trend, string pinPath, string displayName)
	{
		_logger?.LogDebug(
			"Adding pen to trend. PinPath='{PinPath}', DisplayName='{DisplayName}'",
			pinPath,
			displayName);

		var pin = _project.SafeItem<ITreePinHlp>(pinPath);
		if (pin == null)
		{
			_logger?.LogError("Pin not found for path '{PinPath}'", pinPath);
			return Result.Fail($"Pin not found: {pinPath}");
		}

		if (!HasRightsToAddPen(trend))
		{
			_logger?.LogError("No rights to add pen to trend. PinPath='{PinPath}'", pinPath);
			return Result.Fail("No rights to add pen to trend");
		}

		var graph = trend.AddParametr(pin);
		if (graph == null)
		{
			_logger?.LogError("Trend.AddParametr returned null for pin '{PinPath}'", pinPath);
			return Result.Fail("Failed to add pen to trend");
		}

		var configureResult = ConfigurePenSettings(trend, pin, displayName);
		if (configureResult.IsFailed)
		{
			return configureResult;
		}

		return Result.Ok();
	}

	public Trend? FindOpenTrend(TrendService trendService, string trendFullName)
	{
		foreach (var trend in trendService.Opened)
		{
			if (trend.Attribute?.TreeItem == null)
			{
				continue;
			}

			if (string.Equals(
					trend.Attribute.TreeItem.FullName,
					trendFullName,
					StringComparison.OrdinalIgnoreCase))
			{
				return trend;
			}
		}

		return null;
	}

	public void ConfigureTrendCapacity(Trend trend, int maxParameters)
	{
		trend.Settings.MaxParameters = maxParameters;
	}

	private static bool HasRightsToAddPen(Trend trend)
	{
		if (!trend.AddParamsRights)
		{
			return false;
		}

		if (!trend.RuntimeMode || !trend.Inited)
		{
			return true;
		}

		return trend.CheckPermissionTrend(Rights.Trends.AddParams, "Добавление пера");
	}

	private static Result ConfigurePenSettings(Trend trend, ITreePinHlp pin, string userName)
	{
		var pinId = trend.Attribute.GetPinId(pin);
		if (string.IsNullOrWhiteSpace(pinId))
		{
			return Result.Fail("Failed to get pin ID");
		}

		var graph = trend.Settings.Objects
			.OfType<BaseGraph2D>()
			.FirstOrDefault(g =>
			{
				var settings = g.CustomSettings as ScadaPenSettings;
				return settings != null &&
					   string.Equals(
						   settings.PinId,
						   pinId,
						   StringComparison.OrdinalIgnoreCase);
			});

		if (graph == null)
		{
			return Result.Fail("Pen not found in trend");
		}

		SetPenUserName(graph, userName);
		SetPenStaircaseStyle(graph);
		trend.UpdateVisibleSources();

		return Result.Ok();
	}

	private static void SetPenUserName(BaseGraph2D graph, string userName)
	{
		var penSettings = graph.CustomSettings as ScadaPenSettings;
		if (penSettings != null)
		{
			penSettings.UserName = userName;
			penSettings.SavedDT = false;
		}
	}

	private static void SetPenStaircaseStyle(BaseGraph2D graph)
	{
		graph.SegmentStyle = GraphSegmentStyles.Ступенька;
	}
}
