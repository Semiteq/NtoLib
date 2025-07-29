using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Recipe;

public interface IFbActionTarget
{
    Dictionary<int, string> GetShutterNames();  
    Dictionary<int, string> GetHeaterNames();
    Dictionary<int, string> GetNitrogenSourceNames();
}