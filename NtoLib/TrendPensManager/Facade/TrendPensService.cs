using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using MasterSCADA;
using MasterSCADA.Common;
using MasterSCADA.Graph.Objects;
using MasterSCADA.Hlp;
using MasterSCADA.Hlp.Events;
using MasterSCADA.Trend.Controls;
using MasterSCADA.Trend.Helpers;
using MasterSCADA.Trend.Services;

using MasterSCADALib;

using Microsoft.Extensions.Logging;

using NtoLib.TrendPensManager.Entities;
using NtoLib.TrendPensManager.Services;

namespace NtoLib.TrendPensManager.Facade;

public class TrendPensService : ITrendPensService
{
	private readonly IProjectHlp _project;
	private readonly TreeTraversalService _treeTraversal;
	private readonly ConfigLoaderReader _configReader;
	private readonly PenSequenceBuilder _sequenceBuilder;
	private readonly ILogger<TrendPensService> _logger;

	private sealed record ApplySequenceResult(int PensAdded, List<string> Errors);

	public TrendPensService(
		IProjectHlp project,
		ILogger<TrendPensService> logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		_treeTraversal = new TreeTraversalService(project, loggerFactory: null);
		_configReader = new ConfigLoaderReader(project, loggerFactory: null);
		_sequenceBuilder = new PenSequenceBuilder(loggerFactory: null);
	}

	public TrendPensService(
		IProjectHlp project,
		TreeTraversalService treeTraversal,
		ConfigLoaderReader configReader,
		PenSequenceBuilder sequenceBuilder,
		ILogger<TrendPensService> logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_treeTraversal = treeTraversal ?? throw new ArgumentNullException(nameof(treeTraversal));
		_configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
		_sequenceBuilder = sequenceBuilder ?? throw new ArgumentNullException(nameof(sequenceBuilder));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result<AutoConfigResult> AutoConfigurePens(string trendRootPath, string configLoaderPath)
	{
		_logger.LogInformation(
			"Starting AutoConfigurePens. TrendRootPath='{TrendRootPath}', ConfigLoaderPath='{ConfigLoaderPath}'",
			trendRootPath,
			configLoaderPath);

		try
		{
			var allWarnings = new List<string>();

			_logger.LogDebug("Traversing services under trend root path '{TrendRootPath}'", trendRootPath);
			var traversalResult = GetChannels(trendRootPath);
			if (traversalResult.IsFailed)
			{
				_logger.LogError(
					"Service traversal failed for trend root '{TrendRootPath}'. Errors: {Errors}",
					trendRootPath,
					string.Join("; ", traversalResult.Errors.Select(e => e.Message)));

				return Result.Fail(traversalResult.Errors);
			}

			var traversalData = traversalResult.Value;
			allWarnings.AddRange(traversalData.Warnings);
			var channels = traversalData.Channels;

			_logger.LogInformation(
				"Service traversal completed. ChannelsFound={ChannelsCount}, Warnings={WarningsCount}",
				channels.Count,
				traversalData.Warnings.Count);

			_logger.LogDebug("Reading ConfigLoader outputs from path '{ConfigLoaderPath}'", configLoaderPath);
			var configResult = GetConfigLoaderNames(configLoaderPath);
			if (configResult.IsFailed)
			{
				_logger.LogError(
					"ConfigLoader reading failed for path '{ConfigLoaderPath}'. Errors: {Errors}",
					configLoaderPath,
					string.Join("; ", configResult.Errors.Select(e => e.Message)));

				return Result.Fail(configResult.Errors);
			}

			var configNames = configResult.Value;
			_logger.LogInformation(
				"ConfigLoader outputs read successfully. ServiceTypes={ServiceTypes}",
				string.Join(", ", configNames.Keys));

			_logger.LogDebug(
				"Building pen sequence. Channels={ChannelsCount}, TrendPath='{TrendRootPath}'",
				channels.Count,
				trendRootPath);

			var sequenceResult = BuildPenSequence(channels, configNames, trendRootPath);
			if (sequenceResult.IsFailed)
			{
				_logger.LogError(
					"Pen sequence building failed for trend root '{TrendRootPath}'. Errors: {Errors}",
					trendRootPath,
					string.Join("; ", sequenceResult.Errors.Select(e => e.Message)));

			 return Result.Fail(sequenceResult.Errors);
			}

			var sequenceData = sequenceResult.Value;
			allWarnings.AddRange(sequenceData.Warnings);

			_logger.LogInformation(
				"Pen sequence built. PensInSequence={PensCount}, Warnings={WarningsCount}",
				sequenceData.Sequence.Count,
				sequenceData.Warnings.Count);

			_logger.LogDebug(
				"Applying pen sequence to trend '{TrendRootPath}'. SequenceCount={SequenceCount}",
				trendRootPath,
				sequenceData.Sequence.Count);

			var applyResult = ApplySequenceToTrend(trendRootPath, sequenceData.Sequence);
			if (applyResult.IsFailed)
			{
				_logger.LogError(
					"Applying pen sequence to trend failed for '{TrendRootPath}'. Errors: {Errors}",
					trendRootPath,
					string.Join("; ", applyResult.Errors.Select(e => e.Message)));

				return Result.Fail(applyResult.Errors);
			}

			var applySequenceResult = applyResult.Value;
			var pensAdded = applySequenceResult.PensAdded;

			if (applySequenceResult.Errors.Count > 0)
			{
				_logger.LogError(
					"Pen application finished with errors. PensAdded={PensAdded}, Errors={Errors}",
					pensAdded,
					string.Join("; ", applySequenceResult.Errors));

				return Result.Fail(applySequenceResult.Errors);
			}

			var usedChannelsCount = channels.Count(c => c.Used);
			var result = new AutoConfigResult(usedChannelsCount, pensAdded, allWarnings);

			_logger.LogInformation(
				"AutoConfigurePens completed successfully. UsedChannels={UsedChannels}, PensAdded={PensAdded}, Warnings={WarningsCount}",
				usedChannelsCount,
				pensAdded,
				allWarnings.Count);

			return Result.Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception during AutoConfigurePens.");
			return Result.Fail($"AutoConfig error: {ex.Message}");
		}
	}

	private Result<TraversalData> GetChannels(string trendRootPath)
	{
		return _treeTraversal.TraverseServices(trendRootPath);
	}

	private Result<Dictionary<ServiceType, string[]>> GetConfigLoaderNames(string configLoaderPath)
	{
		return _configReader.ReadOutputs(configLoaderPath);
	}

	private Result<PenSequenceData> BuildPenSequence(
		List<ChannelInfo> channels,
		Dictionary<ServiceType, string[]> configNames,
		string trendRootPath)
	{
		return _sequenceBuilder.BuildSequence(channels, configNames, trendPath: trendRootPath);
	}

	private Result<ApplySequenceResult> ApplySequenceToTrend(
		string trendRootPath,
		IReadOnlyCollection<PenSequenceItem> sequence)
	{
		var trendItem = _project.SafeItem<ITreeItemHlp>(trendRootPath);
		if (trendItem == null)
		{
			_logger.LogWarning(
				"Trend item not found for path '{TrendRootPath}' while applying pen sequence.",
				trendRootPath);

			return Result.Fail($"Trend object not found: {trendRootPath}");
		}

		var trendService = _project.GetService<TrendService>();
		if (trendService == null || TrendService.Dispatcher == null)
		{
			_logger.LogError(
				"TrendService or its Dispatcher is not available when applying pen sequence. TrendRootPath='{TrendRootPath}'",
				trendRootPath);

		 return Result.Fail("Trend service is not available");
		}

		var pensAdded = 0;
		var errors = new List<string>();

		TrendService.Dispatcher.Invoke(() =>
		{
			_logger.LogDebug(
				"Dispatcher invoked to apply pen sequence. TrendRootPath='{TrendRootPath}', SequenceCount={SequenceCount}",
				trendRootPath,
				sequence.Count);

			var trend = FindOpenTrendForItem(trendService, trendItem.FullName);
			if (trend == null)
			{
				_logger.LogError(
					"Trend window for item '{TrendItemFullName}' is not open.",
					trendItem.FullName);

				errors.Add("Trend window is closed");
				return;
			}

			SetMaxTrendItems(trend, 1000);

			foreach (var item in sequence)
			{
				var addResult = AddPenToTrend(trend, item.SourcePinPath, item.PenDisplayName);
				if (addResult.IsSuccess)
				{
					pensAdded++;
					_logger.LogDebug(
						"Pen added successfully. SourcePinPath='{SourcePinPath}', DisplayName='{DisplayName}'",
						item.SourcePinPath,
						item.PenDisplayName);
				}
				else
				{
					var errorText = string.Join(", ", addResult.Errors.Select(e => e.Message));
					errors.Add($"{item.SourcePinPath}: {errorText}");

					_logger.LogError(
						"Failed to add pen. SourcePinPath='{SourcePinPath}', Errors='{Errors}'",
						item.SourcePinPath,
						errorText);
				}
			}
		});

		return Result.Ok(new ApplySequenceResult(pensAdded, errors));
	}

	private Result AddPenToTrend(Trend trend, string pinPath, string displayName)
	{
		_logger.LogDebug("Adding pen to trend. PinPath='{PinPath}', DisplayName='{DisplayName}'", pinPath, displayName);

		var pin = _project.SafeItem<ITreePinHlp>(pinPath);
		if (pin == null)
		{
			_logger.LogError("Pin not found for path '{PinPath}'", pinPath);
			return Result.Fail($"Pin not found: {pinPath}");
		}

		if (!HasRightsToAddPen(trend))
		{
			_logger.LogError("No rights to add pen to trend. PinPath='{PinPath}'", pinPath);
			return Result.Fail("No rights to add pen to trend");
		}

		var graph = trend.AddParametr(pin);
		if (graph == null)
		{
			_logger.LogError("Trend.AddParametr returned null for pin '{PinPath}'", pinPath);
			return Result.Fail("Failed to add pen to trend");
		}

		return SetPenUserName(trend, pin, displayName);
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

	private static Trend? FindOpenTrendForItem(TrendService trendService, string trendFullName)
	{
		foreach (var trend in trendService.Opened)
		{
			if (trend.Attribute?.TreeItem == null)
			{
				continue;
			}

			if (string.Equals(trend.Attribute.TreeItem.FullName, trendFullName, StringComparison.OrdinalIgnoreCase))
			{
				return trend;
			}
		}

		return null;
	}

	private static Result SetPenUserName(Trend trend, ITreePinHlp pin, string userName)
	{
		var pinId = trend.Attribute.GetPinId(pin);
		if (string.IsNullOrWhiteSpace(pinId))
		{
			return Result.Fail("Failed to get pin ID");
		}

		var penSettings = trend.Settings.Objects
			.OfType<BaseGraph2D>()
			.Select(g => g.CustomSettings as ScadaPenSettings)
			.FirstOrDefault(s => s != null &&
								 string.Equals(s.PinId, pinId, StringComparison.OrdinalIgnoreCase));

		if (penSettings == null)
		{
			return Result.Fail("Pen not found in trend");
		}

		penSettings.UserName = userName;
		penSettings.SavedDT = false;
		trend.UpdateVisibleSources();

		return Result.Ok();
	}

	private static void SetMaxTrendItems(Trend trend, int max = 1000)
	{
		trend.Settings.MaxParameters = max;
	}
}
