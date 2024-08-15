using FB.VisualFB;
using NtoLib.Devices.Pumps;
using NtoLib.Devices.Valves;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Utils
{
    public static class LayoutBuilder
    {
        public static DeviceLayout BuildLayout(VisualControlBase control, bool noButtons)
        {
            Size elementSize = new Size();
            Size tableSize = new Size();

            Point elementLocation = new Point();
            Point tableLocation = new Point();

            if(control is ValveControl valveControl)
            {
                (elementSize, tableSize) = CalculateValveSize(valveControl, noButtons);

                Render.Orientation buttonOrientation;
                if(IsHorizontal(valveControl.Orientation))
                {
                    if(valveControl.ButtonOrientation == ButtonOrientation.RigthBottom)
                        buttonOrientation = Render.Orientation.Bottom;
                    else
                        buttonOrientation = Render.Orientation.Top;
                }
                else
                {
                    if(valveControl.ButtonOrientation == ButtonOrientation.RigthBottom)
                        buttonOrientation = Render.Orientation.Right;
                    else
                        buttonOrientation = Render.Orientation.Left;
                }
                (elementLocation, tableLocation) = CalculatePositions((elementSize, tableSize), buttonOrientation);
            }
            else if(control is PumpControl pumpControl)
            {
                (elementSize, tableSize) = CalculatePumpSize(pumpControl, noButtons); 
                
                Render.Orientation buttonOrientation;
                if(pumpControl.IsHorizontal())
                {
                    if(pumpControl.ButtonOrientation == ButtonOrientation.RigthBottom)
                        buttonOrientation = Render.Orientation.Right;
                    else
                        buttonOrientation = Render.Orientation.Left;
                }
                else
                {
                    if(pumpControl.ButtonOrientation == ButtonOrientation.RigthBottom)
                        buttonOrientation = Render.Orientation.Bottom;
                    else
                        buttonOrientation = Render.Orientation.Top;
                }
                (elementLocation, tableLocation) = CalculatePositions((elementSize, tableSize), buttonOrientation);
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

        public static void RebuildTable(TableLayoutPanel table, Render.Orientation orientation, Button[] buttons)
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



        private static (Size valveSize, Size tableSize) CalculateValveSize(ValveControl valve, bool noButtons)
        {
            Rectangle bounds = valve.Bounds;

            Size valveSize = new Size();
            Size tableSize = new Size();

            if(noButtons)
            {
                valveSize = bounds.Size;
                tableSize = new Size();
                return (valveSize, tableSize);
            }

            float hwRatio;
            if(valve.IsSlideGate)
                hwRatio = 3f / 4f;
            else
                hwRatio = 2f / 3f;

            if(IsHorizontal(valve.Orientation))
            {
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

                tableSize.Height = bounds.Height - valveSize.Height - 1;
                tableSize.Width = valveSize.Width;
            }
            else
            {
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
                tableSize.Width = bounds.Width - valveSize.Width - 1;
            }

            return (valveSize, tableSize);
        }

        private static (Size pumpSize, Size tableSize) CalculatePumpSize(PumpControl pump, bool noButtons)
        {
            Rectangle bounds = pump.Bounds;

            Size pumpSize = new Size();
            Size tableSize = new Size();

            int pumpHeight = Math.Min(bounds.Height, bounds.Width);
            pumpSize = new Size(pumpHeight, pumpHeight);

            if(noButtons)
            {
                tableSize = new Size();
                return (pumpSize, tableSize);
            }
            
            if(pumpHeight == bounds.Width)
            {
                tableSize.Height = bounds.Height - pumpSize.Height - 1;
                tableSize.Width = pumpSize.Width;
            }
            else
            {
                tableSize.Height = pumpSize.Height;
                tableSize.Width = bounds.Width - pumpSize.Width - 1;
            }

            return (pumpSize, tableSize);
        }

        private static (Point ElementLocation, Point TableLocation) CalculatePositions((Size Element, Size Table) size, Render.Orientation tableOrientation)
        {
            Point valveLocation = new Point();
            Point tableLocation = new Point();

            switch(tableOrientation)
            {
                case Render.Orientation.Bottom:
                {
                    valveLocation.X = 0;
                    tableLocation.X = 0;

                    valveLocation.Y = 0;
                    tableLocation.Y = size.Element.Height;

                    break;
                }
                case Render.Orientation.Right:
                {
                    valveLocation.Y = 0;
                    tableLocation.Y = 0;

                    valveLocation.X = 0;
                    tableLocation.X = size.Element.Width;

                    break;
                }
                case Render.Orientation.Top:
                {
                    valveLocation.X = 0;
                    tableLocation.X = 0; 
                    
                    valveLocation.Y = size.Table.Height;
                    tableLocation.Y = 0;

                    break;
                }
                case Render.Orientation.Left:
                {
                    valveLocation.Y = 0;
                    tableLocation.Y = 0; 
                    
                    valveLocation.X = size.Table.Width;
                    tableLocation.X = 0;

                    break;
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
