using System;
using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.Recipe.Actions
{
    public class ActionTarget
    {
        private Dictionary<int, string> _targetNames = new();
        
        public void SetNames(Dictionary<int, string> names)
        {
            _targetNames = names ?? throw new ArgumentNullException(nameof(names));
        }

        public string GetName(int targetId)
        {
            if (_targetNames.TryGetValue(targetId, out var name))
            {
                return name;
            }
            return string.Empty;
        }
        
        public int GetId(string targetName)
        {
            if (_targetNames.ContainsValue(targetName))
            {
                return _targetNames.FirstOrDefault(x => x.Value == targetName).Key;
            }
            throw new KeyNotFoundException($"Target name '{targetName}' not found.");
        }
        
        public int GetMinimalId()
        {
            if (_targetNames.Count == 0)
                throw new InvalidOperationException("Target names dictionary is empty.");
            
            return _targetNames.Keys.Min();
        }
        
        public int GetDefaultId()
        {
            if (_targetNames.Count == 0)
                throw new InvalidOperationException("Target names dictionary is empty.");
            
            return _targetNames.First().Key;
        }
        
        public IEnumerable<KeyValuePair<int, string>> GetAllItems()
        {
            return _targetNames.ToList();
        }
        
        public Dictionary<int, string> GetNamesDictionary()
        {
            return new Dictionary<int, string>(_targetNames);
        }
    }
}
