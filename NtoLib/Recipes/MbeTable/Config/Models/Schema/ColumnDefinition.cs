#nullable enable

using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Config.Models.Schema
{
    /// <summary>
    /// Represents the definition of a column in a data grid or table. This record holds metadata
    /// necessary to construct and configure a column, such as its key, index, display name, type,
    /// width, and alignment.
    /// </summary>
    /// <param name="Key">The unique column key that identifies a column. Represented as a type-safe identifier.</param>
    /// <param name="Index">The zero-based position of the column in the table.</param>
    /// <param name="Code">A string that may serve as an internal identifier or code for the column, for IO purposes.</param>
    /// <param name="UiName">The user-facing name of the column, Displayed as the column header text in a table.</param>
    /// <param name="SystemType">The data type associated with the values stored in the column. This defines the kind of data the column accepts.</param>
    /// <param name="Width">Specifies the width of the column in pixels. -1 for auto</param>
    /// <param name="ReadOnly">Indicates whether the column is read-only, meaning the column is computed only.</param>
    /// <param name="Alignment">Defines the alignment of content within each cell in the column, such as Left, Center, or Right alignment.</param>
    public record ColumnDefinition(
        ColumnIdentifier Key,
        int Index,
        string Code,
        string UiName,
        Type SystemType,
        int Width,
        bool ReadOnly,
        DataGridViewContentAlignment Alignment,
        PlcMapping? PlcMapping
    );
}