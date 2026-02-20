using System;
using System.Linq;

using FluentResults;

using MasterSCADA;
using MasterSCADA.Hlp;
using MasterSCADA.Trend.Controls;
using MasterSCADA.Trend.Services;
using MasterSCADA.Trend.Windows;

using MasterSCADALib;

using Microsoft.Extensions.Logging;

namespace NtoLib.TrendPensManager.Services;

public class TrendWindowAccessor
{
	private const int MaxPensPerTrendLimit = 1000;
	private readonly ILogger<TrendWindowAccessor>? _logger;

	private readonly IProjectHlp _project;
	private readonly TrendOperations _trendOperations;

	public TrendWindowAccessor(
		IProjectHlp project,
		TrendOperations trendOperations,
		ILogger<TrendWindowAccessor>? logger = null)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_trendOperations = trendOperations ?? throw new ArgumentNullException(nameof(trendOperations));
		_logger = logger;
	}

	public Result<TrendContext> ResolveContext(string trendRootPath)
	{
		if (string.IsNullOrWhiteSpace(trendRootPath))
		{
			return Result.Fail("Trend root path is empty.");
		}

		var trendService = _project.GetService<TrendService>();
		if (trendService == null)
		{
			_logger?.LogError("TrendService is not available when resolving context. TrendRootPath='{TrendRootPath}'",
				trendRootPath);

			return Result.Fail("Trend service is not available");
		}

		var dispatcher = TrendService.Dispatcher;
		if (dispatcher == null)
		{
			_logger?.LogError("TrendService.Dispatcher is null when resolving context. TrendRootPath='{TrendRootPath}'",
				trendRootPath);

			return Result.Fail("Trend dispatcher is not available");
		}

		var treeItem = _project.SafeItem<ITreeItemHlp>(trendRootPath);
		if (treeItem == null)
		{
			_logger?.LogError("Trend tree item not found for path '{TrendRootPath}'", trendRootPath);

			return Result.Fail($"Trend tree item not found: {trendRootPath}");
		}

		var attribute =
			treeItem.Attributes.FirstOrDefault(a =>
				a.Type == EDocType.dtTrend && a.TreeItem.FullName == treeItem.FullName)
			?? treeItem.Attributes.FirstOrDefault(a => a.Type == EDocType.dtTrend);
		if (attribute == null)
		{
			_logger?.LogError("Trend attribute not found for path '{TrendRootPath}'", trendRootPath);

			return Result.Fail($"Trend attribute not found: {trendRootPath}");
		}

		return Result.Ok(new TrendContext(trendService, dispatcher, treeItem, attribute));
	}

	public Result<Trend> GetTrend(TrendContext trendContext)
	{
		var trend = _trendOperations.FindOpenTrend(trendContext.TrendService, trendContext.TreeItem.FullName);
		if (trend == null)
		{
			_logger?.LogError("Trend window is not open. TrendFullName='{TrendFullName}'",
				trendContext.TreeItem.FullName);

			return Result.Fail("Trend window is not open");
		}

		_logger?.LogDebug("Trend window found. TrendFullName='{TrendFullName}'", trendContext.TreeItem.FullName);

		return Result.Ok(trend);
	}

	[Obsolete("Method directly uses project tree. Behavior is untested. Use GetTrend instead.")]
	public Result<Trend> GetOrOpenTrend(TrendContext context)
	{
		var trend = _trendOperations.FindOpenTrend(context.TrendService, context.TreeItem.FullName);
		if (trend != null)
		{
			_logger?.LogDebug("Trend window already open. TrendFullName='{TrendFullName}'", context.TreeItem.FullName);

			return Result.Ok(trend);
		}

		_logger?.LogInformation("Opening trend window. TrendFullName='{TrendFullName}'", context.TreeItem.FullName);
		var openParameters = new OpenTrendParameters
		{
			Attribute = context.Attribute,
			Item = context.TreeItem,
			DocType = EDocType.dtTrend,
			AutoScroll = false,
			TopMost = false
		};

		var openedWindow = context.TrendService.Open(openParameters);
		if (openedWindow is not TrendWindow trendWindow || trendWindow.Trend == null)
		{
			_logger?.LogError("Trend window failed to open or has no Trend control. TrendFullName='{TrendFullName}'",
				context.TreeItem.FullName);

			return Result.Fail("Failed to open trend window");
		}

		trend = trendWindow.Trend;
		_logger?.LogInformation("Trend window opened. TrendFullName='{TrendFullName}'", context.TreeItem.FullName);

		return Result.Ok(trend);
	}

	public void EnsureCapacityLimit(Trend trend)
	{
		trend.Settings.MaxParameters = MaxPensPerTrendLimit;
		_logger?.LogDebug("Trend capacity limit applied. Limit={Limit}", MaxPensPerTrendLimit);
	}

	public Result ClearExistingGraphs(Trend trend)
	{
		var clearResult = _trendOperations.ClearTrendData(trend);
		if (clearResult.IsFailed)
		{
			_logger?.LogError("Failed to clear existing graphs in trend. Errors='{Errors}'",
				string.Join(", ", clearResult.Errors));

			return clearResult;
		}

		_logger?.LogDebug("Existing graphs cleared in trend.");

		return Result.Ok();
	}
}
