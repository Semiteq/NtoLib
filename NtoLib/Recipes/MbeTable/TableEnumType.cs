using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable
{
    internal class TableEnumType
    {
        private Dictionary<string, int> _map;
        private string _name;

        public TableEnumType(string name)
        {
            this._name = name;
            this._map = new Dictionary<string, int>();
        }

        public string Name => this._name;

        public void AddEnum(string str, int value)
        {
            if (string.IsNullOrEmpty(str))
                return;
            if (this._map.ContainsKey(str))
                this._map[str] = value;
            else
                this._map.Add(str, value);
        }

        public int enum_counts => this._map.Count;


        public int? GetActionNumber(string action)
        {
            if (_map.ContainsKey(action))
                return _map[action];
            else
                return null;
        }

        public string GetNameByIterrator(int ittr_num)
        {
            int num = 0;
            foreach (KeyValuePair<string, int> pair in this._map)
            {
                if (num == ittr_num)
                    return pair.Key;
                ++num;
            }
            return "";
        }

        public int get_value_by_ittr_num(int ittr_num)
        {
            int num = 0;
            foreach (KeyValuePair<string, int> pair in this._map)
            {
                if (num == ittr_num)
                    return pair.Value;
                ++num;
            }
            return 0;
        }

        public string GetNameByNumber(int num)
        {
            foreach (KeyValuePair<string, int> pair in this._map)
            {
                if (pair.Value == num)
                    return pair.Key;
            }
            return "";
        }

        public int get_by_str(string str) => this._map.ContainsKey(str) ? this._map[str] : 0;

        public bool TryParse(string str, out int val)
        {
            if (this._map.ContainsKey(str))
            {
                val = this._map[str];
                return true;
            }
            val = 0;
            return false;
        }
    }
}