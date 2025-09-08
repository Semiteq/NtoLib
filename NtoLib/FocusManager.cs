using FB.VisualFB;
using System;

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
            Focused?.Invoke(focusedControl);
        }
    }
}
