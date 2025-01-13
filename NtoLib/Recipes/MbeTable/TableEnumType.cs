using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable
{
    internal class TableEnumType : IEnumerable<KeyValuePair<string, int>>
    {
        /// <summary>
        /// Расширение стандартного enum до таблицы которая связывает строки с числами. 
        /// Позволяет добавлять, обновлять и получать значения по ключу или значению, 
        /// а также поддерживает перебор элементов с помощью foreach.
        /// </summary>

        private readonly Dictionary<string, int> _items;
        public int EnumCount => _items.Count;

        public TableEnumType()
        {
            _items = new Dictionary<string, int>();
        }

        // Реализуем метод Add для Collection Initializer, вместо AddEnum
        public void Add(string key, int value)
        {
            if (_items.ContainsKey(key))
                throw new ArgumentException($"Key '{key}' already exists.");
            _items[key] = value;
        }

        public void AddRange(IEnumerable<KeyValuePair<string, int>> items)
        {
            // Проверка на пустую коллекцию
            if (items == null || !items.Any()) return;
            foreach (var item in items)
                Add(item.Key, item.Value);
        }

        public bool IsEmpty => (_items.Count == 0);

        // Обращение по индексу вместо GetActionNumber
        public int this[string key] => _items.ContainsKey(key) ? _items[key] : 0;

        public string GetValueByIndex(int index)
        {
            if (index < 0 || index >= _items.Count)
                //return string.Empty;
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            return _items.ElementAt(index).Key;
        }

        // Реализация IEnumerable для поддержки foreach и collection initializer
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _items.GetEnumerator();

        // Поддержка IEnumerator для не-генерик версии
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Аддитивность 
        public static TableEnumType operator +(TableEnumType left, TableEnumType right)
        {
            var result = new TableEnumType();
            result.AddRange(left);

            foreach (var item in right)
            {
                if (!result._items.ContainsKey(item.Key))
                    result.Add(item.Key, item.Value);
                else
                    throw new ArgumentException($"Duplicate key detected: {item.Key}");
            }

            return result;
        }

        public int GetLowestNumber()
        {
            if (_items.Count == 0)
                return 0;
            return _items.Values.Min();
        }
    }
}
