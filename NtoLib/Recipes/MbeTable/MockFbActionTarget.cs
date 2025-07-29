using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe;

namespace NtoLib.Recipes.MbeTable
{
    /// <summary>
    /// A mock implementation of IFbActionTarget for use in design mode,
    /// preventing null reference exceptions when the real FB is not available.
    /// </summary>
    public class MockFbActionTarget : IFbActionTarget
    {
        public Dictionary<int, string> GetShutterNames() => new Dictionary<int, string>();
        public Dictionary<int, string> GetHeaterNames() => new Dictionary<int, string>();
        public Dictionary<int, string> GetNitrogenSourceNames() => new Dictionary<int, string>();
    }
}