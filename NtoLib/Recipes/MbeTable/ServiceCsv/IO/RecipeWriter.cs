using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;
using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.IO;

/// <summary>
/// Coordinates the recipe writing pipeline to CSV format.
/// </summary>
public sealed class RecipeWriter : IRecipeWriter
{
    private readonly ICsvDataFormatter _dataFormatter;
    private readonly IMetadataService _metadataService;
    private readonly IIntegrityService _integrityService;
    private readonly ILogger _logger;

    public RecipeWriter(
        ICsvDataFormatter dataFormatter,
        IMetadataService metadataService,
        IIntegrityService integrityService,
        ILogger logger)
    {
        _dataFormatter = dataFormatter ?? throw new ArgumentNullException(nameof(dataFormatter));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Result WriteAsync(Recipe recipe, TextWriter writer)
    {
        _logger.LogDebug($"Starting CSV recipe write with {recipe.Steps.Count} steps");
        
        var formatResult = _dataFormatter.FormatToCsv(recipe);
        if (formatResult.IsFailed)
        {
            _logger.LogError("Failed to format recipe to CSV");
            return formatResult.ToResult();
        }
        
        var csvContent = formatResult.Value;
        var dataRows = ExtractDataRows(csvContent);
        
        _logger.LogDebug($"Formatted {dataRows.Count} data rows");
        
        var bodyHash = _integrityService.CalculateHash(dataRows);
        
        var metadata = new RecipeFileMetadata
        {
            Separator = ';',
            Rows = dataRows.Count,
            BodyHashBase64 = bodyHash,
            Extras = new Dictionary<string, string> 
            { 
                ["ExportedAtLocalTime"] = DateTime.Now.ToString("O") 
            }
        };
        
        _metadataService.WriteMetadata(writer, metadata);
        writer.Write(csvContent);
        
        _logger.LogDebug("Recipe write completed successfully");
        
        return Result.Ok();
    }

    private static List<string> ExtractDataRows(string csvContent)
    {
        var lines = csvContent.Split(new[] { "\r\n" }, StringSplitOptions.None);
        
        if (lines.Length <= 1)
        {
            return new List<string>();
        }
        
        return lines.Skip(1).Where(line => !string.IsNullOrEmpty(line)).ToList();
    }
}