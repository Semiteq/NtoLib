using FB.VisualFB;
using NtoLib.Valves;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Utils
{
    public static class LayoutBuilder
    {
        public static DeviceLayout BuildValveLayout(VisualControlBase control)
        {
            Size elementSize = new Size();
            Size tableSize = new Size();

            Point elementLocation = new Point();
            Point tableLocation = new Point();

            if(control is ValveControl valveControl)
            {
                (elementSize, tableSize) = CalculateValveSize(valveControl);
                (elementLocation, tableLocation) = CalculatePositions((elementSize, tableSize), valveControl.Orientation, valveControl.ButtonOrientation);
            }
            else
            {
                throw new NotImplementedException();
            }

            DeviceLayout layout = new DeviceLayout();
            layout.DeviceRectangle = new Rectangle(elementLocation, elementSize);
            layout.ButtonTableRectangle = new Rectangle(tableLocation, tableSize);
            return layout;
        }

        public static void RebuildTable(TableLayoutPanel table, Render.Orientation orientation, Button[] buttons, int buttonSize)
        {
            if(orientation == Render.Orientation.Top || orientation == Render.Orientation.Bottom)
            {
                for(int i = 0; i < buttons.Length; i++)
                    table.SetRow(buttons[i], 0);

                table.RowCount = 1;
                table.ColumnCount = buttons.Length;

                for(int i = 0; i < buttons.Length; i++)
                {
                    table.ColumnStyles[i].SizeType = SizeType.Percent;
                    table.ColumnStyles[i].Width = 1f / buttons.Length;
                    table.SetColumn(buttons[i], i);
                }
            }
            else
            {
                for(int i = 0; i < buttons.Length; i++)
                    table.SetColumn(buttons[i], 0);

                table.ColumnCount = 1;
                table.RowCount = buttons.Length;

                for(int i = 0; i < buttons.Length; i++)
                {
                    table.RowStyles[i].SizeType = SizeType.Percent;
                    table.RowStyles[i].Height = 1f / buttons.Length;
                    table.SetRow(buttons[i], i);
                }
            }
        }



        private static (Size valveSize, Size tableSize) CalculateValveSize(ValveControl valve)
        {
            Rectangle bounds = valve.Bounds;

            Size valveSize = new Size();
            Size tableSize = new Size();

            float hwRatio;
            if(valve.IsSlideGate)
                hwRatio = 3f / 4f;
            else
                hwRatio = 2f / 3f;

            if(IsHorizontal(valve.Orientation))
            {
                bounds.Height -= valve.ButtonSize;

                int heightFromWidth = (int)(hwRatio * bounds.Width);
                if(heightFromWidth < bounds.Height)
                {
                    valveSize.Height = heightFromWidth;
                    valveSize.Width = bounds.Width;
                }
                else
                {
                    valveSize.Height = bounds.Height;
                    valveSize.Width = (int)((1 / hwRatio) * bounds.Height);
                }

                tableSize.Height = valve.ButtonSize;
                tableSize.Width = valveSize.Width;
            }
            else
            {
                bounds.Width -= valve.ButtonSize;

                int widthFromHeight = (int)(hwRatio * bounds.Height);
                if(widthFromHeight < bounds.Width)
                {
                    valveSize.Height = bounds.Height;
                    valveSize.Width = widthFromHeight;
                }
                else
                {
                    valveSize.Height = (int)((1 / hwRatio) * bounds.Width);
                    valveSize.Width = bounds.Width;
                }

                tableSize.Height = valveSize.Height;
                tableSize.Width = valve.ButtonSize;
            }

            return (valveSize, tableSize);
        }

        private static (Point ElementLocation, Point TableLocation) CalculatePositions((Size Element, Size Table) size, Render.Orientation orientation, ButtonOrientation tableOrientation)
        {
            Point valveLocation = new Point();
            Point tableLocation = new Point();

            if(IsHorizontal(orientation))
            {
                valveLocation.X = 0;
                tableLocation.X = 0;

                if(tableOrientation == ButtonOrientation.LeftTop)
                {
                    valveLocation.Y = size.Table.Height;
                    tableLocation.Y = 0;
                }
                else
                {
                    valveLocation.Y = 0;
                    tableLocation.Y = size.Element.Height;
                }
            }
            else
            {
                valveLocation.Y = 0;
                tableLocation.Y = 0;

                if(tableOrientation == ButtonOrientation.LeftTop)
                {
                    valveLocation.X = size.Table.Width;
                    tableLocation.X = 0;
                }
                else
                {
                    valveLocation.X = 0;
                    tableLocation.X = size.Element.Width;
                }
            }

            return (valveLocation, tableLocation);
        }

        private static bool IsHorizontal(Render.Orientation orientation)
        {
            return orientation == Render.Orientation.Right || orientation == Render.Orientation.Left;
        }
    }
}
