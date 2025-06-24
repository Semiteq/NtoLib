using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Table.UI.TableUpdate;

namespace NtoLib.Recipes.MbeTable.Table;

public class TableManager
{
    /// <summary>
    /// Class responsible for managing DataGridView (table) operations such as line management,
    /// table updates, and schema handling.
    /// </summary>
    
    private readonly DataGridView _table;
    
    private readonly RecipeDataExtractor _recipeDataExtractor;
    
    private readonly TableSchema _tableSchema;
    private readonly TablePainter _tablePainter;
    private readonly UpdateBatcher _updateBatcher;
    private readonly TableColumnManager _tableColumnManager;


    public TableManager(DataGridView table, List<Step> recipe , Dictionary<ColumnKey, int> columnKeyToIndexMap, ColorScheme colorScheme,
        TableSchema tableSchema)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table), @"Table cannot be null.");
        _tableSchema = tableSchema ??
                       throw new ArgumentNullException(nameof(tableSchema), @"Table schema cannot be null.");

        _tablePainter = new TablePainter(table, columnKeyToIndexMap, colorScheme);
        _updateBatcher = new UpdateBatcher(table, _tableSchema);
        
    }

    public void ToDesign()
    {
        
    }

    public void ToRuntime()
    {
        
    }

    public void RemoveLine(int rowIndex)
    {
        _table.Rows.RemoveAt(rowIndex);
        _updateBatcher.RequestRowUpdate(rowIndex);
    }

    public void AddLine(Step step, int rowIndex)
    {
        var newRow = _recipeDataExtractor.ConvertStepToRow(step);
        _table.Rows.Insert(rowIndex, newRow);
        _tablePainter.PaintRow(rowIndex, step);
        _updateBatcher.RequestRowUpdate(rowIndex);
    }

    public void ChangeCellValue()
    {
        
    }
}