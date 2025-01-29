using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable
{
    public class TableEnumType : IEnumerable<KeyValuePair<string, int>>
    {
        /// <summary>
        /// Представляет расширение стандартного перечисления (enum) в виде таблицы, 
        /// которая связывает строки с числами. Поддерживает добавление, обновление 
        /// и получение значений по ключу или значению, а также перебор элементов 
        /// с использованием foreach.
        /// </summary>
        private readonly Dictionary<string, int> _items;

        /// <summary>
        /// Возвращает количество элементов в таблице.
        /// </summary>
        public int EnumCount => _items.Count;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TableEnumType"/>.
        /// </summary>
        public TableEnumType()
        {
            _items = new Dictionary<string, int>();
        }

        /// <summary>
        /// Добавляет новый элемент в таблицу.
        /// </summary>
        /// <param name="key">Ключ элемента.</param>
        /// <param name="value">Значение элемента.</param>
        /// <exception cref="ArgumentException">Выбрасывается, если ключ уже существует в таблице.</exception>
        public void Add(string key, int value)
        {
            if (_items.ContainsKey(key))
                throw new ArgumentException($"Ключ '{key}' уже существует.");
            _items[key] = value;
        }

        /// <summary>
        /// Добавляет коллекцию элементов в таблицу.
        /// </summary>
        /// <param name="items">Коллекция элементов в формате пар ключ-значение.</param>
        public void AddRange(IEnumerable<KeyValuePair<string, int>> items)
        {
            if (items == null || !items.Any()) return;

            foreach (var item in items)
                Add(item.Key, item.Value);
        }

        /// <summary>
        /// Проверяет, является ли таблица пустой.
        /// </summary>
        public bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// Возвращает значение, связанное с указанным ключом.
        /// </summary>
        /// <param name="key">Ключ элемента.</param>
        /// <returns>Значение элемента. Выбрасывает исключение, если ключ не найден.</returns>
        /// <exception cref="KeyNotFoundException">Если указанный ключ отсутствует.</exception>
        public int this[string key]
        {
            get
            {
                if (!_items.ContainsKey(key))
                {
                    throw new KeyNotFoundException($"Ключ \"{key}\" отсутствует в словаре.");
                }
                return _items[key];
            }
        }

        /// <summary>
        /// Возвращает ключ, связанный с указанным значением.
        /// </summary>
        /// <param name="value">Значение элемента.</param>
        /// <returns>Ключ элемента. Выбрасывает исключение, если значение не найдено.</returns>
        /// <exception cref="KeyNotFoundException">Если указанное значение отсутствует.</exception>
        public string this[int value]
        {
            get
            {
                var item = _items.FirstOrDefault(x => x.Value == value);

                if (item.Equals(default(KeyValuePair<string, int>)))
                {
                    throw new KeyNotFoundException($"Отсутствует элемент с значением \"{value}\" в словаре.");
                }

                return item.Key;
            }
        }


        /// <summary>
        /// Возвращает ключ элемента по индексу в таблице.
        /// </summary>
        /// <param name="index">Индекс элемента.</param>
        /// <returns>Ключ элемента.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Выбрасывается, если индекс находится за пределами диапазона.</exception>
        public string GetValueByIndex(int index)
        {
            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Индекс за пределами диапазона.");
            return _items.ElementAt(index).Key;
        }

        /// <summary>
        /// Реализует перечисление элементов таблицы для использования в foreach.
        /// </summary>
        /// <returns>Перечислитель элементов таблицы.</returns>
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _items.GetEnumerator();

        /// <summary>
        /// Реализует перечисление элементов таблицы для не-обобщенной коллекции.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Оператор сложения для объединения двух экземпляров <see cref="TableEnumType"/>.
        /// </summary>
        /// <param name="left">Левая таблица.</param>
        /// <param name="right">Правая таблица.</param>
        /// <returns>Новая таблица, содержащая элементы из обеих таблиц.</returns>
        /// <exception cref="ArgumentException">Выбрасывается, если обнаружены дублирующиеся ключи.</exception>
        public static TableEnumType operator +(TableEnumType left, TableEnumType right)
        {
            var result = new TableEnumType();
            result.AddRange(left);

            foreach (var item in right)
            {
                if (!result._items.ContainsKey(item.Key))
                    result.Add(item.Key, item.Value);
                else
                    throw new ArgumentException($"Найден дубликат ключа: {item.Key}.");
            }

            return result;
        }

        /// <summary>
        /// Возвращает наименьшее значение из таблицы.
        /// Если таблица пуста, возвращает -1.
        /// </summary>
        /// <returns>Наименьшее значение.</returns>
        public int GetLowestNumber()
        {
            return _items.Count != 0 ? _items.Values.Min() : -1;
        }

        // Новый конструктор для инициализации из коллекции
        public TableEnumType(IEnumerable<KeyValuePair<string, int>> items)
        {
            _items = items.ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
