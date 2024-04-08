using NtoLib.Utils;
using System.Drawing;

namespace NtoLib.Valves.Render
{
    internal struct PaintData
    {
        public RectangleF Bounds;
        public float LineWidth;
        public float ErrorLineWidth;
        public float ErrorOffset;
        public Orientation Orientation;
        public Blinker Blinker;

    }
}
