using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using MasterSCADA;
using MasterSCADA.Common;
using MasterSCADA.Graph.Objects;
using MasterSCADA.Graph.Styles;
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
	private readonly TrendPenApplicator _penApplicator;
	private readonly ILogger<TrendPensService> _logger;

	public TrendPensService(
		IProjectHlp project,
		ILogger<TrendPensService> logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		var serviceFilter = new ServiceFilter();
		_treeTraversal = new TreeTraversalService(project, serviceFilter, loggerFactory: null);
		_configReader = new ConfigLoaderReader(project, loggerFactory: null);
		_sequenceBuilder = new PenSequenceBuilder(loggerFactory: null);

		var trendOperations = new TrendOperations(project, logger: null);
		_penApplicator = new TrendPenApplicator(project, trendOperations, logger: null);
	}

	public TrendPensService(
		IProjectHlp project,
		TreeTraversalService treeTraversal,
		ConfigLoaderReader configReader,
		PenSequenceBuilder sequenceBuilder,
		TrendPenApplicator penApplicator,
		ILogger<TrendPensService> logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_treeTraversal = treeTraversal ?? throw new ArgumentNullException(nameof(treeTraversal));
		_configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
		_sequenceBuilder = sequenceBuilder ?? throw new ArgumentNullException(nameof(sequenceBuilder));
		_penApplicator = penApplicator ?? throw new ArgumentNullException(nameof(penApplicator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result<AutoConfigResult> AutoConfigurePens(
		string trendRootPath,
		string configLoaderRootPath,
		string dataRootPath,
		ServiceSelectionOptions serviceSelectionOptions)
	{
		_ = serviceSelectionOptions ?? throw new ArgumentNullException(nameof(serviceSelectionOptions));

		_logger.LogInformation(
			"Starting AutoConfigurePens. TrendRootPath='{TrendRootPath}', ConfigLoaderRootPath='{ConfigLoaderRootPath}', DataRootPath='{DataRootPath}'",
			trendRootPath,
			configLoaderRootPath,
			dataRootPath);

		try
		{
			var allWarnings = new List<string>();

			_logger.LogDebug(
				"Traversing services under data root path '{DataRootPath}'",
				dataRootPath);
			var traversalResult = GetChannels(dataRootPath, serviceSelectionOptions);
			if (traversalResult.IsFailed)
			{
				LogErrors("Service traversal failed for data root '{DataRootPath}'", traversalResult.Errors, dataRootPath);
				return Result.Fail(traversalResult.Errors);
			}

			var traversalData = traversalResult.Value;
			allWarnings.AddRange(traversalData.Warnings);
			var channels = traversalData.Channels;

			_logger.LogInformation(
				"Service traversal completed. ChannelsFound={ChannelsCount}, Warnings={WarningsCount}",
				channels.Count,
				traversalData.Warnings.Count);

			_logger.LogDebug(
				"Reading ConfigLoader outputs from path '{ConfigLoaderRootPath}'",
				configLoaderRootPath);
			var configResult = GetConfigLoaderNames(configLoaderRootPath, serviceSelectionOptions);
			if (configResult.IsFailed)
			{
				LogErrors("ConfigLoader reading failed for path '{ConfigLoaderRootPath}'", configResult.Errors, configLoaderRootPath);
				return Result.Fail(configResult.Errors);
			}

			var configNames = configResult.Value;
			_logger.LogInformation(
				"ConfigLoader outputs read successfully. ServiceTypes={ServiceTypes}",
				string.Join(", ", configNames.Keys));

			_logger.LogDebug(
				"Building pen sequence. Channels={ChannelsCount}, TrendRootPath='{TrendRootPath}'",
				channels.Count,
				trendRootPath);

			var sequenceResult = BuildPenSequence(channels, configNames, trendRootPath);
			if (sequenceResult.IsFailed)
			{
				LogErrors("Pen sequence building failed for trend root '{TrendRootPath}'", sequenceResult.Errors, trendRootPath);
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

			var applyResult = _penApplicator.ApplySequenceToTrend(trendRootPath, sequenceData.Sequence);
			if (applyResult.IsFailed)
			{
				LogErrors("Applying pen sequence to trend failed for '{TrendRootPath}'", applyResult.Errors, trendRootPath);
				return Result.Fail(applyResult.Errors);
			}

			var applyData = applyResult.Value;
			var pensAdded = applyData.PensAdded;

			if (applyData.Errors.Count > 0)
			{
				_logger.LogError(
					"Pen application finished with errors. PensAdded={PensAdded}, Errors={Errors}",
					pensAdded,
					string.Join("; ", applyData.Errors));

				return Result.Fail(applyData.Errors);
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

	private Result<TraversalData> GetChannels(
		string dataRootPath,
		ServiceSelectionOptions serviceSelectionOptions)
	{
		return _treeTraversal.TraverseServices(dataRootPath, serviceSelectionOptions);
	}

	private Result<Dictionary<ServiceType, string[]>> GetConfigLoaderNames(
		string configLoaderRootPath,
		ServiceSelectionOptions serviceSelectionOptions)
	{
		return _configReader.ReadOutputs(configLoaderRootPath, serviceSelectionOptions);
	}

	private Result<PenSequenceData> BuildPenSequence(
		IReadOnlyList<ChannelInfo> channels,
		Dictionary<ServiceType, string[]> configNames,
		string trendRootPath)
	{
		return _sequenceBuilder.BuildSequence(channels, configNames, trendRootPath);
	}

	private void LogErrors(string message, IEnumerable<IError> errors, params object[] args)
	{
		var errorText = string.Join("; ", errors.Select(e => e.Message));
		var fullMessage = $"{message}. Errors: {{Errors}}";
		var allArgs = args.Append(errorText).ToArray();
		_logger.LogError(fullMessage, allArgs);
	}
}
