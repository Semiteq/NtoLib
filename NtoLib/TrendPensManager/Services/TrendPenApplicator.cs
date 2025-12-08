using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using MasterSCADA.Hlp;
using MasterSCADA.Trend.Controls;
using MasterSCADA.Trend.Services;

using Microsoft.Extensions.Logging;

using NtoLib.TrendPensManager.Entities;

namespace NtoLib.TrendPensManager.Services;

public class TrendPenApplicator
{
	private readonly IProjectHlp _project;
	private readonly TrendOperations _trendOperations;
	private readonly ILogger<TrendPenApplicator>? _logger;

	private const int DefaultMaxTrendParameters = 1000;

	public sealed record ApplyResult(int PensAdded, List<string> Errors);

	public TrendPenApplicator(
		IProjectHlp project,
		TrendOperations trendOperations,
		ILogger<TrendPenApplicator>? logger = null)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_trendOperations = trendOperations ?? throw new ArgumentNullException(nameof(trendOperations));
		_logger = logger;
	}

	public Result<ApplyResult> ApplySequenceToTrend(
		string trendRootPath,
		IReadOnlyCollection<PenSequenceItem> sequence)
	{
		var trendItem = _project.SafeItem<ITreeItemHlp>(trendRootPath);
		if (trendItem == null)
		{
			_logger?.LogWarning(
				"Trend item not found for path '{TrendRootPath}' while applying pen sequence.",
				trendRootPath);

			return Result.Fail($"Trend object not found: {trendRootPath}");
		}

		var trendService = _project.GetService<TrendService>();
		if (trendService == null || TrendService.Dispatcher == null)
		{
			_logger?.LogError(
				"TrendService or its Dispatcher is not available when applying pen sequence. TrendRootPath='{TrendRootPath}'",
				trendRootPath);

			return Result.Fail("Trend service is not available");
		}

		var pensAdded = 0;
		var errors = new List<string>();

		TrendService.Dispatcher.Invoke(() =>
		{
			_logger?.LogDebug(
				"Dispatcher invoked to apply pen sequence. TrendRootPath='{TrendRootPath}', SequenceCount={SequenceCount}",
				trendRootPath,
				sequence.Count);

			var trend = _trendOperations.FindOpenTrend(trendService, trendItem.FullName);
			if (trend == null)
			{
				_logger?.LogError(
					"Trend window for item '{TrendItemFullName}' is not open.",
					trendItem.FullName);

				errors.Add("Trend window is closed");
				return;
			}

			_trendOperations.ConfigureTrendCapacity(trend, DefaultMaxTrendParameters);

			foreach (var item in sequence)
			{
				var addResult = _trendOperations.AddPenToTrend(trend, item.SourcePinPath, item.PenDisplayName);
				if (addResult.IsSuccess)
				{
					pensAdded++;
					_logger?.LogDebug(
						"Pen added successfully. SourcePinPath='{SourcePinPath}', DisplayName='{DisplayName}'",
						item.SourcePinPath,
						item.PenDisplayName);
				}
				else
				{
					var errorText = string.Join(", ", addResult.Errors.Select(e => e.Message));
					errors.Add($"{item.SourcePinPath}: {errorText}");

					_logger?.LogError(
						"Failed to add pen. SourcePinPath='{SourcePinPath}', Errors='{Errors}'",
						item.SourcePinPath,
						errorText);
				}
			}
		});

		return Result.Ok(new ApplyResult(pensAdded, errors));
	}
}
