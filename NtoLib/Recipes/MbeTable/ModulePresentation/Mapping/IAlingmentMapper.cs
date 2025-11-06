using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

public interface IAlignmentMapper
{
    DataGridViewContentAlignment Map(UiAlignment value);
}