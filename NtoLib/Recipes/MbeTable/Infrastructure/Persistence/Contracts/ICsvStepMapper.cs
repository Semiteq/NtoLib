#nullable enable

using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

public interface ICsvStepMapper
{
    /// <summary>
    /// Maps a CSV record to a <see cref="Step"/> object while validating and handling errors.
    /// </summary>
    /// <param name="lineNumber">The line number of the CSV record, used for error tracking.</param>
    /// <param name="record">The CSV record as an array of strings.</param>
    /// <param name="binding">The header binding that maps column keys to record indices.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the mapped <see cref="Step"/> object on success, or an error on failure.
    /// </returns>
    Result<Step> FromRecord(
        int lineNumber,
        string[] record,
        CsvHeaderBinder.Binding binding);

    /// <summary>
    /// Converts a <see cref="Step"/> object into a CSV record as an array of strings based on the given column order.
    /// </summary>
    /// <param name="step">The <see cref="Step"/> object containing properties to map to a CSV record.</param>
    /// <param name="orderedColumns">A list of <see cref="ColumnDefinition"/> objects defining the order and structure of the columns.</param>
    /// <returns>
    /// An array of strings representing the CSV record corresponding to the <see cref="Step"/> object, with values mapped according to the column definitions.
    /// </returns>
    string[] ToRecord(Step step, IReadOnlyList<ColumnDefinition> orderedColumns);
}