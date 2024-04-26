using NtoLib.Valves;
using System.Drawing;

namespace NtoLib.Render.Valves
{
    internal abstract class ValveBaseRenderer : BaseRenderer
    {
        /// <summary>Экземпляр ValveControl, к которому привязан данный Renderer</summary>
        protected ValveControl _control { get; private set; }



        public ValveBaseRenderer(ValveControl valveControl)
        {
            _control = valveControl;
        }



        /// <summary>
        /// Возвращает цвета первого и второго треугольника в зависимости от статуса
        /// </summary>
        protected Color[] GetValveColors(Status status, bool isLight)
        {
            Color[] colors = new Color[2];

            if(status.State == State.NoData)
            {
                colors[0] = Colors.NoData;
                colors[1] = Colors.NoData;
            }
            else if(status.State == State.Opened)
            {
                colors[0] = Colors.Opened;
                colors[1] = Colors.Opened;
            }
            else if(status.State == State.Closed || status.State == State.SmothlyOpened)
            {
                colors[0] = Colors.Closed;
                colors[1] = Colors.Closed;
            }
            else
            {
                if(isLight)
                {
                    colors[0] = Colors.Opened;
                    colors[1] = Colors.Closed;
                }
                else
                {
                    colors[0] = Colors.Closed;
                    colors[1] = Colors.Opened;
                }
            }

            return colors;
        }

        /// <summary>
        /// Возвращет истину, если должна быть показана блокировка
        /// </summary>
        protected bool IsBlocked(Status status)
        {
            if(status.UsedByAutoMode)
                return true;
            if(status.State == State.Opened && status.BlockClosing)
                return true;
            if(status.State == State.Closed && status.BlockOpening)
                return true;
            if(status.State == State.SmothlyOpened && (status.BlockClosing || status.BlockOpening))
                return true;

            return false;
        }
    }
}
