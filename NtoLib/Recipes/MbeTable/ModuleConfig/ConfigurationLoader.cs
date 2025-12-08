using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleConfig.Mapping;
using NtoLib.Recipes.MbeTable.ModuleConfig.Validation;
using NtoLib.Recipes.MbeTable.ModuleConfig.YamlConfig;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleConfig;

/// <summary>
/// High-level facade for loading and validating application configuration from YAML files.
/// Throws ConfigException at boundary on any failure.
/// </summary>
public sealed class ConfigurationLoader : IConfigurationLoader
{
	private const string DefaultPropertyDefsFileName = "PropertyDefs.yaml";
	private const string DefaultColumnDefsFileName = "ColumnDefs.yaml";
	private const string DefaultPinGroupDefsFileName = "PinGroupDefs.yaml";
	private const string DefaultActionsDefsFileName = "ActionsDefs.yaml";

	private readonly IYamlDeserializer _yamlDeserializer;
	private readonly CrossReferenceValidator _crossReferenceValidator;
	private readonly FormulaDefsValidator _formulaDefsValidator;
	private readonly PropertyDefinitionMapper _propertyMapper;
	private readonly ColumnDefinitionMapper _columnMapper;
	private readonly ActionDefinitionMapper _actionMapper;
	private readonly PinGroupDataMapper _pinGroupMapper;

	public ConfigurationLoader()
		: this(
			new YamlDeserializer(),
			new CrossReferenceValidator(new NumberParser()),
			new FormulaDefsValidator(),
			new PropertyDefinitionMapper(),
			new ColumnDefinitionMapper(),
			new ActionDefinitionMapper(),
			new PinGroupDataMapper())
	{
	}

	public ConfigurationLoader(
		IYamlDeserializer yamlDeserializer,
		CrossReferenceValidator crossReferenceValidator,
		FormulaDefsValidator formulaDefsValidator,
		PropertyDefinitionMapper propertyMapper,
		ColumnDefinitionMapper columnMapper,
		ActionDefinitionMapper actionMapper,
		PinGroupDataMapper pinGroupMapper)
	{
		_yamlDeserializer = yamlDeserializer ?? throw new ArgumentNullException(nameof(yamlDeserializer));
		_crossReferenceValidator =
			crossReferenceValidator ?? throw new ArgumentNullException(nameof(crossReferenceValidator));
		_formulaDefsValidator = formulaDefsValidator ?? throw new ArgumentNullException(nameof(formulaDefsValidator));
		_propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
		_columnMapper = columnMapper ?? throw new ArgumentNullException(nameof(columnMapper));
		_actionMapper = actionMapper ?? throw new ArgumentNullException(nameof(actionMapper));
		_pinGroupMapper = pinGroupMapper ?? throw new ArgumentNullException(nameof(pinGroupMapper));
	}

	public AppConfiguration LoadConfiguration(string configurationDirectory, params string[] fileNames)
	{
		var result = TryLoadConfiguration(configurationDirectory, fileNames);
		if (result.IsSuccess)
			return result.Value;

		var errors = result.Errors.OfType<ConfigError>().ToArray();
		if (errors.Length == 0)
		{
			errors = result.Errors
				.Select(e =>
					new ConfigError(e.Message, "loader", "boundary").WithDetail("rawErrorType", e.GetType().Name))
				.ToArray();
		}

		throw new ConfigException(errors);
	}

	public AppConfiguration LoadConfiguration(string configurationDirectory)
		=> LoadConfiguration(
			configurationDirectory,
			DefaultPropertyDefsFileName,
			DefaultColumnDefsFileName,
			DefaultPinGroupDefsFileName,
			DefaultActionsDefsFileName);

	private Result<AppConfiguration> TryLoadConfiguration(string configurationDirectory, params string[] fileNames)
	{
		var dirResult = EnsureConfigurationDirectoryExists(configurationDirectory);
		if (dirResult.IsFailed)
			return dirResult.ToResult<AppConfiguration>();

		var fileResult = EnsureConfigurationFilesExist(configurationDirectory, fileNames);
		if (fileResult.IsFailed)
			return fileResult.ToResult<AppConfiguration>();

		var configurationFiles = new ConfigurationFiles(configurationDirectory, fileNames);

		try
		{
			var allConfigFilesResult = LoadAllConfigFiles(configurationFiles);
			if (allConfigFilesResult.IsFailed)
				return allConfigFilesResult.ToResult<AppConfiguration>();

			var allConfigFiles = allConfigFilesResult.Value;

			var crossRefResult = _crossReferenceValidator.Validate(allConfigFiles);
			if (crossRefResult.IsFailed)
				return crossRefResult.ToResult<AppConfiguration>();

			var appConfig = ConvertAndAssemble(allConfigFiles);

			var formulaValidationResult = ValidateFormulas(appConfig);
			if (formulaValidationResult.IsFailed)
				return formulaValidationResult.ToResult<AppConfiguration>();

			return Result.Ok(appConfig);
		}
		catch (Exception ex)
		{
			return Result.Fail(new ConfigError(
				$"Unexpected error while loading configuration: {ex.Message}",
				section: "loader",
				context: "try-load",
				cause: ex));
		}
	}

	private static Result EnsureConfigurationDirectoryExists(string configurationDirectory)
	{
		return !Directory.Exists(configurationDirectory)
			? Result.Fail(new ConfigError(
					$"Path not found: {configurationDirectory}",
					section: "filesystem",
					context: "directory-check")
				.WithDetail("baseDirectory", configurationDirectory))
			: Result.Ok();
	}

	private static Result EnsureConfigurationFilesExist(string configurationDirectory, params string[] fileNames)
	{
		if (fileNames == null || fileNames.Length < 4)
		{
			return Result.Fail(new ConfigError(
					"At least 4 configuration file names are required.",
					section: "filesystem",
					context: "files-check")
				.WithDetail("baseDirectory", configurationDirectory));
		}

		var missingFiles = fileNames
			.Where(fileName => !File.Exists(Path.Combine(configurationDirectory, fileName)))
			.ToArray();

		return missingFiles.Any()
			? Result.Fail(new ConfigError(
					$"Missing configuration files: {string.Join(", ", missingFiles)}",
					section: "filesystem",
					context: "files-check")
				.WithDetail("baseDirectory", configurationDirectory))
			: Result.Ok();
	}

	private Result<CombinedYamlConfig> LoadAllConfigFiles(ConfigurationFiles files)
	{
		var propertyDefsResult = LoadPropertyDefsFile(files.PropertyDefsPath);
		if (propertyDefsResult.IsFailed)
			return propertyDefsResult.ToResult();

		var columnDefsResult = LoadColumnDefs(files.ColumnDefsPath);
		if (columnDefsResult.IsFailed)
			return columnDefsResult.ToResult();

		var pinGroupDefsResult = LoadPinGroupDefs(files.PinGroupDefsPath);
		if (pinGroupDefsResult.IsFailed)
			return pinGroupDefsResult.ToResult();

		var actionDefsResult = LoadActionDefs(files.ActionDefsPath);
		if (actionDefsResult.IsFailed)
			return actionDefsResult.ToResult();

		var sections = new CombinedYamlConfig(
			propertyDefsResult.Value,
			columnDefsResult.Value,
			pinGroupDefsResult.Value,
			actionDefsResult.Value);

		return Result.Ok(sections);
	}

	private Result<PropertyDefsYamlConfig> LoadPropertyDefsFile(string path)
	{
		var validator = new PropertyDefsValidator();
		var loader = new FileLoader<YamlPropertyDefinition>(_yamlDeserializer, validator);

		return loader.Load(path).Map(items => new PropertyDefsYamlConfig(items));
	}

	private Result<ColumnDefsYamlConfig> LoadColumnDefs(string path)
	{
		var validator = new ColumnDefsValidator();
		var loader = new FileLoader<YamlColumnDefinition>(_yamlDeserializer, validator);

		return loader.Load(path).Map(items => new ColumnDefsYamlConfig(items));
	}

	private Result<PinGroupDefsYamlConfig> LoadPinGroupDefs(string path)
	{
		var validator = new PinGroupDefsValidator();
		var loader = new FileLoader<YamlPinGroupDefinition>(_yamlDeserializer, validator);

		return loader.Load(path).Map(items => new PinGroupDefsYamlConfig(items));
	}

	private Result<ActionDefsYamlConfig> LoadActionDefs(string path)
	{
		var validator = new ActionDefsValidator();
		var loader = new FileLoader<YamlActionDefinition>(_yamlDeserializer, validator);

		var loadResult = loader.Load(path);
		if (loadResult.IsFailed)
			return loadResult.ToResult();

		var actions = loadResult.Value;

		foreach (var action in actions.Where(a => a.Formula != null))
		{
			var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";
			var formulaValidationResult = _formulaDefsValidator.Validate(action.Formula!, context);
			if (formulaValidationResult.IsFailed)
				return formulaValidationResult.ToResult<ActionDefsYamlConfig>();
		}

		return Result.Ok(new ActionDefsYamlConfig(actions));
	}

	private AppConfiguration ConvertAndAssemble(CombinedYamlConfig files)
	{
		var propertyDefinitions = new Dictionary<string, IPropertyTypeDefinition>(StringComparer.OrdinalIgnoreCase);

		foreach (var yamlDef in files.PropertyDefs.Items)
		{
			var mapped = _propertyMapper.Map(yamlDef);
			propertyDefinitions[yamlDef.PropertyTypeId] = mapped;
		}

		var columnDefinitions = _columnMapper.MapMany(files.ColumnDefs.Items);
		var actionDefinitions = _actionMapper.MapMany(files.ActionDefs.Items).ToDictionary(a => a.Id);
		var pinGroupData = _pinGroupMapper.MapMany(files.PinGroupDefs.Items);

		return new AppConfiguration(propertyDefinitions, columnDefinitions, actionDefinitions, pinGroupData);
	}

	private static Result ValidateFormulas(AppConfiguration appConfig)
	{
		foreach (var action in appConfig.Actions.Values.Where(a => a.Formula != null))
		{
			var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";
			var validator = new FormulaValidator(action.Formula, action.Columns, context);
			var validationResult = validator.Validate();
			if (validationResult.IsFailed)
				return validationResult;
		}

		return Result.Ok();
	}
}
