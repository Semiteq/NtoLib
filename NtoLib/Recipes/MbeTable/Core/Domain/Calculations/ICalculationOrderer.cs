#nullable enable
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Calculations;

public interface ICalculationOrderer
{
    Result<IReadOnlyList<ColumnDefinition>> OrderAndValidate(IReadOnlyList<ColumnDefinition> allColumns);
    IReadOnlyList<ColumnDefinition> GetCalculatedColumnsInOrder();
    IReadOnlyDictionary<ColumnIdentifier, string> GetColumnKeyToDataTableNameMap();
}