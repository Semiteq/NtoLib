using FB.VisualFB;
using System;
using System.Windows.Forms;

namespace NtoLib
{
    public static class FocusManager
    {
        /// <summary>
        /// Вызывается при смене фокуса. 
        /// В качестве параметра передаётся объект, 
        /// который теперь в фокусе, либо null
        /// </summary>
        public static event Action<VisualControlBase> Focused;

        public static void OnFocused(VisualControlBase focusedControl)
        {
            try
            {
                // Проверяем, что объект не освобожден перед вызовом события
                if (focusedControl != null && focusedControl is Control control)
                {
                    if (control.IsDisposed)
                        return;
                }
                
                Focused?.Invoke(focusedControl);
            }
            catch (ObjectDisposedException)
            {
                // Игнорируем исключения для освобожденных объектов
            }
        }
    }
}
