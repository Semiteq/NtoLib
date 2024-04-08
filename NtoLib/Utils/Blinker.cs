using System;
using System.Threading.Tasks;

namespace NtoLib.Utils
{
    public class Blinker : IDisposable
    {
        public bool IsLight;
        public event Action OnLightChanged;

        private bool _isDisposed;



        public Blinker(int msBlinkDuration)
        {
            Task.Run(() => LightChangeByTime(msBlinkDuration));
        }



        private async Task LightChangeByTime(int msBlinkDuration)
        {
            while(!_isDisposed)
            {
                await Task.Delay(msBlinkDuration);
                IsLight = !IsLight;

                OnLightChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
