using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable
{
    public class TableEnumType : IEnumerable<KeyValuePair<string, int>>
    {
        /// <summary>
        /// Represents an extended enum-like table mapping strings to integers.
        /// Supports adding, updating, retrieving values by key or value,
        /// and iterating over elements using foreach.
        /// </summary>
        private readonly Dictionary<string, int> _items;

        /// <summary>
        /// Gets the number of elements in the table.
        /// </summary>
        public int EnumCount => _items.Count;

        /// <summary>
        /// Initializes a new instance of <see cref="TableEnumType"/>.
        /// </summary>
        public TableEnumType()
        {
            _items = new Dictionary<string, int>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TableEnumType"/> from a collection.
        /// </summary>
        public TableEnumType(IEnumerable<KeyValuePair<string, int>> items)
        {
            _items = items.ToDictionary(item => item.Key, item => item.Value);
        }

        /// <summary>
        /// Adds a new element to the table.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the key already exists.</exception>
        public void Add(string key, int value)
        {
            if (_items.ContainsKey(key))
                throw new ArgumentException($"Key '{key}' already exists.");
            _items[key] = value;
        }

        /// <summary>
        /// Adds a collection of elements to the table.
        /// </summary>
        public void AddRange(IEnumerable<KeyValuePair<string, int>> items)
        {
            if (items == null || !items.Any()) return;

            foreach (var item in items)
                Add(item.Key, item.Value);
        }

        /// <summary>
        /// Checks if the table is empty.
        /// </summary>
        public bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if the key is not found.</exception>
        public int this[string key] => _items.TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"Key '{key}' not found.");

        /// <summary>
        /// Gets the key associated with the specified value.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if the value is not found.</exception>
        public string this[int value] => _items.FirstOrDefault(x => x.Value == value) is { Key: not null } item
            ? item.Key
            : throw new KeyNotFoundException($"Value '{value}' not found.");

        /// <summary>
        /// Gets the key at the specified index in the table.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        public string GetValueByIndex(int index)
        {
            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            return _items.ElementAt(index).Key;
        }

        public bool TryGetValue(string key, out int value) => _items.TryGetValue(key, out value);

        /// <summary>
        /// Gets the lowest number from the table. Returns -1 if the table is empty.
        /// </summary>
        public int GetLowestNumber() => _items.Count != 0 ? _items.Values.Min() : -1;

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Merges two <see cref="TableEnumType"/> instances.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if duplicate keys are found.</exception>
        public static TableEnumType operator +(TableEnumType left, TableEnumType right)
        {
            var result = new TableEnumType();
            result.AddRange(left);

            foreach (var item in right)
            {
                if (!result._items.ContainsKey(item.Key))
                    result.Add(item.Key, item.Value);
                else
                    throw new ArgumentException($"Duplicate key found: {item.Key}.");
            }

            return result;
        }
    }
}
