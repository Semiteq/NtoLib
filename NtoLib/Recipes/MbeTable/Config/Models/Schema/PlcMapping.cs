#nullable enable

namespace NtoLib.Recipes.MbeTable.Config.Models.Schema
{
    /// <summary>
    /// Describes the mapping of a table column to a PLC memory area.
    /// This is a DTO class for deserialization purposes.
    /// </summary>
    public class PlcMapping
    {
        /// <summary>
        /// The memory area in the PLC (e.g., "Int", "Float").
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// The zero-based index (offset) within that memory area for a single recipe row.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlcMapping"/> class.
        /// Required for JSON deserialization.
        /// </summary>
        public PlcMapping()
        {
            Area = string.Empty;
        }
    }
}