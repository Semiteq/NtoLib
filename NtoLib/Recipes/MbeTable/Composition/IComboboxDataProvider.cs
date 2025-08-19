using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Composition;

public interface IComboboxDataProvider
{
    List<KeyValuePair<int, string>> GetActionTargets(int actionId);
    List<KeyValuePair<int, string>> GetActions();
}