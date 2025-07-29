using System;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Contracts
{
    /// <summary>
    /// Интерфейс для свойств, совместимых с TableControl
    /// </summary>
    public interface ITableProperty
    {
        /// <summary>
        /// Тип свойства
        /// </summary>
        PropertyType Type { get; }
        
        /// <summary>
        /// Заблокировано ли свойство для редактирования
        /// </summary>
        bool IsBlocked { get; }
        
        /// <summary>
        /// Получить строковое представление значения для отображения в таблице
        /// </summary>
        string GetDisplayValue();
        
        /// <summary>
        /// Получить сырое значение без форматирования
        /// </summary>
        string GetRawValue();
        
        /// <summary>
        /// Установить значение из строки (для ввода в таблице)
        /// </summary>
        /// <param name="value">Строковое значение</param>
        /// <param name="errorMessage">Сообщение об ошибке, если установка не удалась</param>
        /// <returns>true, если значение установлено успешно</returns>
        bool TrySetValueFromString(string value, out string errorMessage);
        
        /// <summary>
        /// Валидация текущего значения
        /// </summary>
        /// <param name="errorMessage">Сообщение об ошибке валидации</param>
        /// <returns>true, если значение валидно</returns>
        bool IsValid(out string errorMessage);
        
        /// <summary>
        /// Привести значение к указанному типу
        /// </summary>
        /// <param name="targetType">Целевой тип</param>
        /// <returns>Значение, приведенное к целевому типу</returns>
        object CastTo(Type targetType);
        
        /// <summary>
        /// Клонировать свойство
        /// </summary>
        ITableProperty Clone();
    }
}