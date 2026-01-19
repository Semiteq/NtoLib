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
	private readonly TrendWindowAccessor _trendWindowAccessor;
	private readonly TrendOperations _trendOperations;
	private readonly ILogger<TrendPenApplicator>? _logger;

	public sealed record ApplyResult(int PensAdded, List<string> Errors);

	public TrendPenApplicator(
		TrendWindowAccessor trendWindowAccessor,
		TrendOperations trendOperations,
		ILogger<TrendPenApplicator>? logger = null)
	{
		_trendWindowAccessor = trendWindowAccessor ?? throw new ArgumentNullException(nameof(trendWindowAccessor));
		_trendOperations = trendOperations ?? throw new ArgumentNullException(nameof(trendOperations));
		_logger = logger;
	}

	public Result<ApplyResult> ApplySequenceToTrend(
		string trendRootPath,
		IReadOnlyCollection<PenSequenceItem> sequence)
	{
		var contextResult = _trendWindowAccessor.ResolveContext(trendRootPath);
		if (contextResult.IsFailed)
		{
			return Result.Fail(contextResult.Errors);
		}

		var context = contextResult.Value;
		var pensAdded = 0;
		var errors = new List<string>();

		context.Dispatcher.Invoke(() =>
		{
			_logger?.LogDebug(
				"Dispatcher invoked to apply pen sequence. TrendRootPath='{TrendRootPath}', SequenceCount={SequenceCount}",
				trendRootPath,
				sequence.Count);

			var trendResult = _trendWindowAccessor.GetTrend(context);
			if (trendResult.IsFailed)
			{
				errors.AddRange(trendResult.Errors.Select(e => e.Message));
				return;
			}

			var trend = trendResult.Value;

			_trendWindowAccessor.EnsureCapacityLimit(trend);

			var clearResult = _trendWindowAccessor.ClearExistingGraphs(trend);
			if (clearResult.IsFailed)
			{
				errors.AddRange(clearResult.Errors.Select(e => e.Message));
				return;
			}

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
