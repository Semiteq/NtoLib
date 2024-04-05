using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Valves.Render
{
    internal struct PaintData
    {
        public RectangleF Bounds;
        public float LineWidth;
        public float ErrorLineWidth;
        public float ErrorOffset;
        public Orientation Orientation;
        public Shape Shape;

    }
}
