using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services
{
    /// <summary>
    /// Provides access to the configured schema of the recipe table.
    /// The schema is loaded from an external source and provided during construction.
    /// </summary>
    public class TableSchema
    {
        private readonly IReadOnlyList<ColumnDefinition> _columns;
        private readonly Dictionary<ColumnIdentifier, ColumnDefinition> _columnsByKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchema"/> class with a specific set of column definitions.
        /// </summary>
        /// <param name="columns">The list of column definitions that define the table structure.</param>
        /// <exception cref="ArgumentNullException">Thrown if the columns collection is null.</exception>
        public TableSchema(IReadOnlyList<ColumnDefinition> columns)
        {
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            _columnsByKey = _columns.ToDictionary(c => c.Key);
        }

        /// <summary>
        /// Gets the complete list of column definitions for the table.
        /// </summary>
        /// <returns>A read-only list of column definitions.</returns>
        public IReadOnlyList<ColumnDefinition>  GetColumns() => _columns;

        /// <summary>
        /// Gets a specific column definition by its zero-based index.
        /// </summary>
        /// <param name="index">The index of the column to retrieve.</param>
        /// <returns>The <see cref="NtoLib.Recipes.MbeTable.Config.TableSchema.ColumnDefinition"/> at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of bounds.</exception>
        public ColumnDefinition GetColumnDefinition(int index)
        {
            if (index < 0 || index >= _columns.Count)
                throw new IndexOutOfRangeException("Invalid column index.");

            return _columns[index];
        }
        
        /// <summary>
        /// Gets a specific column definition by its unique identifier.
        /// </summary>
        /// <param name="key">The identifier of the column to retrieve.</param>
        /// <returns>The <see cref="NtoLib.Recipes.MbeTable.Config.TableSchema.ColumnDefinition"/> for the specified key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no column with the given key is found.</exception>
        public ColumnDefinition GetColumnDefinition(ColumnIdentifier key)
        {
            return _columnsByKey.TryGetValue(key, out var definition) 
                ? definition
                : throw new KeyNotFoundException($"Column with key '{key.Value}' not found in schema.");
        }
    }
}