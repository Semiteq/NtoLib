

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Domain;
using NtoLib.Recipes.MbeTable.Config.Sections;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// High-level facade for loading and validating application configuration from YAML files.
/// Handles file system checks, error reporting via MessageBox, and creates ConfigurationState.
/// </summary>
public sealed class ConfigurationBootstrapperFacade
{
    private readonly ConfigurationBootstrapper _configurationBootstrapper;

    public ConfigurationBootstrapperFacade()
    {
        _configurationBootstrapper = new ConfigurationBootstrapper();
    }

    /// <summary>
    /// Loads and validates configuration from the specified directory.
    /// Shows MessageBox with errors and throws exception if loading fails.
    /// </summary>
    public ConfigurationState LoadConfiguration(
        string configurationDirectory,
        string propertyDefsFileName,
        string columnDefsFileName,
        string pinGroupDefsFileName,
        string actionsDefsFileName)
    {
        EnsureConfigurationDirectoryExists(configurationDirectory);
        EnsureConfigurationFilesExist(
            configurationDirectory,
            propertyDefsFileName,
            columnDefsFileName,
            pinGroupDefsFileName,
            actionsDefsFileName);

        var configurationFiles = new ConfigurationFiles(
            configurationDirectory,
            propertyDefsFileName,
            columnDefsFileName,
            pinGroupDefsFileName,
            actionsDefsFileName);

        var loadResult = _configurationBootstrapper.LoadConfiguration(configurationFiles);
        if (loadResult.IsFailed)
        {
            HandleLoadFailure(loadResult);
        }

        var (sections, appConfiguration) = loadResult.Value;
        return new ConfigurationState(sections, appConfiguration);
    }

    private static void EnsureConfigurationDirectoryExists(string configurationDirectory)
    {
        if (!Directory.Exists(configurationDirectory))
        {
            var errorMessage = $"Configuration directory not found: '{configurationDirectory}'.";
            MessageBox.Show(errorMessage, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static void EnsureConfigurationFilesExist(
        string configurationDirectory,
        params string[] fileNames)
    {
        var missingFiles = fileNames
            .Select(fileName => Path.Combine(configurationDirectory, fileName))
            .Where(fullPath => !File.Exists(fullPath))
            .Select(Path.GetFileName)
            .ToArray();

        if (missingFiles.Length > 0)
        {
            var errorMessage = $"Configuration files not found in '{configurationDirectory}':\n{string.Join("\n", missingFiles)}";
            MessageBox.Show(errorMessage, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static void HandleLoadFailure(
        Result<(ConfigurationSections Sections, AppConfiguration AppConfiguration)> result)
    {
        var errorReport = new StringBuilder();
        errorReport.AppendLine("Failed to load configuration due to the following errors:");
        errorReport.AppendLine();

        foreach (var error in result.Errors)
        {
            errorReport.AppendLine($"• {error.Message}");

            if (error.Reasons.Any())
            {
                foreach (var reason in error.Reasons)
                {
                    errorReport.AppendLine($"  - {reason.Message}");
                }
            }
        }

        var errorText = errorReport.ToString();
        MessageBox.Show(errorText, "Configuration Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        throw new InvalidOperationException(errorText);
    }
}