using System;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table;

public class RecipeDataExtractor
{
    private readonly TableSchema _tableSchema;
    private readonly ActionTarget _shutters;
    private readonly ActionTarget _heaters;
    private readonly ActionTarget _nitrogenSources;
    
    public RecipeDataExtractor(TableSchema tableSchema, ActionTarget shutters, ActionTarget heaters, ActionTarget nitrogenSources)
    {
        _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema), @"Table schema cannot be null.");
        _shutters = shutters;
        _heaters = heaters;
        _nitrogenSources = nitrogenSources;
    }
    
    public DataGridViewRow ConvertStepToRow(Step step)
    {
        var row = new DataGridViewRow();
        
        var cells = new DataGridViewCell[_tableSchema.GetColumnCount()];
        
        
        foreach (var column in _tableSchema.GetReadonlyColumns())
        {
            DataGridViewCell cell;
        
            if (step.ReadOnlyProperties.TryGetValue(column.Key, out var property))
            {
                if (property.Type == PropertyType.Enum)
                {
                    cell = new DataGridViewComboBoxCell()
                    {
                        Value = property.GetFormattedValue()
                    };
                }
                else
                {
                    cell = new DataGridViewTextBoxCell()
                    {
                        Value = property.GetFormattedValue()
                    };
                }
            }
            else
            {
                // Consider Enum typed cells can't be empty
                cell = new DataGridViewTextBoxCell();
            }
        
            cells[column.Index] = cell;
        }
        
        row.Cells.AddRange(cells);
    
        return row;
    }
    
    private DataGridViewComboBoxCell CreateComboBoxCell(string columnKey, Property property)
    {
        var comboCell = new DataGridViewComboBoxCell();

        // if (actionTarget != null)
        // {
        //     comboCell.DataSource = actionTarget.GetAllItems().ToList();
        //     comboCell.DisplayMember = "Value";
        //     comboCell.ValueMember = "Key";
        // }
    
        comboCell.Value = property.GetFormattedValue();
        return comboCell;
    } 
}