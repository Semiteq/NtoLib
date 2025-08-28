#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv.Hasher;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;

/// <summary>
/// A serializer for handling CSV-based persistence of recipe data.
/// </summary>
public sealed class RecipeCsvSerializerV1 : IRecipeSerializer
{
    private readonly TableSchema _schema;
    // --- ЗАМЕНИТЬ ---
    // private readonly ActionManager _actionManager;
    private readonly IActionRepository _actionRepository;
    private readonly ICsvHelperFactory _csvHelperFactory;
    private readonly RecipeFileMetadataSerializer _recipeFileMetadataSerializer;
    private readonly ICsvHeaderBinder _csvHeaderBinder;
    private readonly ICsvStepMapper _csvStepMapper;
    private readonly IRecipeLoopValidator _recipeLoopValidator;
    private readonly TargetAvailabilityValidator _targetAvailabilityValidator;
    private readonly IActionTargetProvider _actionTargetProvider;

    public RecipeCsvSerializerV1(
        TableSchema schema,
        IActionRepository actionRepository,
        ICsvHelperFactory csvFactory,
        RecipeFileMetadataSerializer metaSerializer,
        ICsvHeaderBinder headerBinder,
        ICsvStepMapper csvStepMapper,
        IRecipeLoopValidator loopValidator,
        TargetAvailabilityValidator targetsValidator,
        IActionTargetProvider targetProvider)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _csvHelperFactory = csvFactory ?? throw new ArgumentNullException(nameof(csvFactory));
        _recipeFileMetadataSerializer = metaSerializer ?? throw new ArgumentNullException(nameof(metaSerializer));
        _csvHeaderBinder = headerBinder ?? throw new ArgumentNullException(nameof(headerBinder));
        _csvStepMapper = csvStepMapper ?? throw new ArgumentNullException(nameof(csvStepMapper));
        _recipeLoopValidator = loopValidator ?? throw new ArgumentNullException(nameof(loopValidator));
        _targetAvailabilityValidator = targetsValidator ?? throw new ArgumentNullException(nameof(targetsValidator));
        _actionTargetProvider = targetProvider ?? throw new ArgumentNullException(nameof(targetProvider));
    }

    /// <summary>
    /// Deserializes a recipe from the provided text reader, parsing metadata, body text,
    /// and performing validation checks for the recipe structure and content integrity.
    /// </summary>
    /// <param name="reader">The text reader containing the serialized recipe data.</param>
    /// <returns>A <see cref="Result{T}"/> containing the deserialized recipe object if successful, or a list of errors.</returns>
    public Result<Recipe> Deserialize(TextReader reader)
    {
        var fullText = reader.ReadToEnd();

        // Metadata
        var (meta, metaLines, signatureFound, versionFound) = _recipeFileMetadataSerializer.ReadAllMeta(fullText);

        if (!signatureFound)
            return Result.Fail(new RecipeError("Missing or invalid signature line (expected '# MBE-RECIPE v=...')"));

        if (!versionFound)
            return Result.Fail(new RecipeError("Missing version in signature line (expected 'v=...')"));
        
        if (meta.Version != 1)
            return Result.Fail(new RecipeError($"Unsupported version {meta.Version}"));

        // Body as text
        var bodyText = ExtractBodyText(fullText, metaLines);

        // CSV parsing (header + data)
        using var csv = _csvHelperFactory.CreateReader(new StringReader(bodyText));
        if (!csv.Read())
            return Result.Fail(new RecipeError("Missing header"));

        csv.ReadHeader();
        var headerTokens = csv.HeaderRecord ?? Array.Empty<string>();
        var bindResult = _csvHeaderBinder.Bind(headerTokens, _schema);
        if (bindResult.IsFailed)
            return bindResult.ToResult();

        // Check separator
        if (meta.Separator != _csvHelperFactory.Separator)
            return Result.Fail(new RecipeError(
                $"Separator mismatch: meta='{meta.Separator}' vs expected='{_csvHelperFactory.Separator}'"));

        // Read rows and compute hash on the fly to avoid storing all rows in memory
        var steps = ImmutableList.CreateBuilder<Step>();
        var bodyHasher = new BodyIntegrityHasher();
        var rowsCount = 0;
        var lineNo = 1; // header

        while (csv.Read())
        {
            lineNo++;
            var record = csv.Context.Reader.Parser.Record ?? Array.Empty<string>();

            // Canonicalize the row exactly as we write it (same delimiter/quoting/newline rules)
            var canonicalRow = BuildCanonicalRowLine(record);
            bodyHasher.AppendDataRow(canonicalRow);
            rowsCount++;

            var stepResult = _csvStepMapper.FromRecord(lineNo, record, bindResult.Value);
            if (stepResult.IsFailed)
                return stepResult.ToResult();

            steps.Add(stepResult.Value);
        }

        var recipe = new Recipe(steps.ToImmutable());

        // Check consistency ROWS + HASH
        if (meta.Rows != 0 && meta.Rows != rowsCount)
            return Result.Fail(new RecipeError($"Rows mismatch: meta={meta.Rows}, actual={rowsCount}"));

        if (!string.IsNullOrWhiteSpace(meta.BodyHashBase64))
        {
            var computed = bodyHasher.ComputeBase64();
            if (!string.Equals(computed, meta.BodyHashBase64, StringComparison.Ordinal))
                return Result.Fail(new RecipeError("Body hash mismatch"));
        }

        // Validation
        var loopRes = _recipeLoopValidator.Validate(recipe);
        if (!loopRes.IsValid)
            return Result.Fail(new RecipeError(loopRes.ErrorMessage ?? "Loop structure invalid"));

        var targetsResult = _targetAvailabilityValidator.Validate(recipe, _actionRepository, _actionTargetProvider);
        if (targetsResult.IsFailed)
            return targetsResult;

        return Result.Ok(recipe);
    }

    /// <summary>
    /// Serializes the given recipe into CSV format and writes it to the provided text writer.
    /// </summary>
    /// <param name="recipe">The recipe to be serialized into CSV format.</param>
    /// <param name="writer">The text writer to which the serialized CSV data will be written.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result Serialize(Recipe recipe, TextWriter writer)
    {
        var (bodyText, canonicalRowsCount) = BuildBodyText(recipe);

        // Compute BODY_SHA256 over the data rows (after header)
        var dataOnly = ExtractDataOnly(bodyText);
        var hasher = new BodyIntegrityHasher();
        if (!string.IsNullOrEmpty(dataOnly))
        {
            foreach (var row in SplitRows(dataOnly))
                hasher.AppendDataRow(row);
        }

        var bodyHash = hasher.ComputeBase64();

        var meta = new RecipeFileMetadata
        {
            Signature = "MBE-RECIPE",
            Version = 1,
            Separator = _csvHelperFactory.Separator,
            Rows = canonicalRowsCount,
            BodyHashBase64 = bodyHash,
            Extras = new Dictionary<string, string> { ["ExportedAtUtc"] = DateTime.UtcNow.ToString("O") }
        };

        _recipeFileMetadataSerializer.Write(writer, meta);
        writer.Write(bodyText);

        return Result.Ok();
    }

    private string ExtractBodyText(string fullText, int metaLines)
    {
        using var sr = new StringReader(fullText);
        var sb = new StringBuilder();

        for (var i = 0; i < metaLines; i++)
            sr.ReadLine(); // skip meta

        string? line;
        var first = true;

        while ((line = sr.ReadLine()) != null)
        {
            if (!first) sb.Append('\n');
            sb.Append(line);
            first = false;
        }

        return sb.ToString();
    }

    private (string BodyText, int RowsCount) BuildBodyText(Recipe recipe)
    {
        var bodySb = new StringBuilder();

        using var bodySw = new StringWriter(bodySb);
        using var csv = _csvHelperFactory.CreateWriter(bodySw);

        var orderedCols = _schema.GetColumns()
            .Where(c => c.ReadOnly == false)
            .OrderBy(c => c.Index)
            .ToArray();

        // Header
        foreach (var c in orderedCols) csv.WriteField(c.Code);
        csv.NextRecord();

        // Rows
        var rows = 0;
        foreach (var step in recipe.Steps)
        {
            var cells = _csvStepMapper.ToRecord(step, orderedCols);
            foreach (var field in cells) csv.WriteField(field);
            csv.NextRecord();
            rows++;
        }

        return (bodySb.ToString(), rows);
    }

    private string ExtractDataOnly(string bodyText)
    {
        var nlIdx = bodyText.IndexOf("\r\n", StringComparison.Ordinal);
        if (nlIdx < 0) return string.Empty;
        return bodyText.Substring(nlIdx + 2);
    }

    private IEnumerable<string> SplitRows(string dataOnly)
    {
        var parts = dataOnly.Split(new[] { "\r\n" }, StringSplitOptions.None);
        for (var i = 0; i < parts.Length - 1; i++)
            yield return parts[i];
    }

    private string BuildCanonicalRowLine(string[] record)
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        using var w = _csvHelperFactory.CreateWriter(sw);
        foreach (var f in record) w.WriteField(f);
        w.NextRecord();
        return sb.ToString().TrimEnd('\r', '\n');
    }
}