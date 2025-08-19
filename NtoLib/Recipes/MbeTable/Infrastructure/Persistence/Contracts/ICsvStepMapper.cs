using System.Collections.Generic;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

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
    /// A tuple containing the mapped <see cref="Step"/> object (or null if mapping fails) and a list of errors encountered during mapping.
    /// </returns>
    (Step Step, IImmutableList<RecipeFileError> Errors) FromRecord(
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