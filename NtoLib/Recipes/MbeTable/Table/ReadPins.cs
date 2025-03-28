﻿using System.Collections.Generic;
using FB.VisualFB;
using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class ReadPins : VisualFBBase
    {
        private int StartPin { get; }
        private int Quantity { get; }

        // Necessary to pass state into FB, otherwise Null-reference error
        private VisualFBBase FB { get; }

        public ReadPins(int startPin, int quantity, VisualFBBase fb)
        {
            StartPin = startPin;
            Quantity = quantity;
            FB = fb;
        }

        public Dictionary<int,string> ReadPinNames()
        {
            Dictionary<int,string> names = new();

            for (var i = 0; i < Quantity; i++)
            {
                string pinString = FB.GetPinValue<string>(i + StartPin);
                if (!string.IsNullOrEmpty(pinString))
                {
                    names.Add(i + 1, pinString);
                }
            }

            return names;
        }

        public bool IsPinGroupQualityGood()
        {
            for (var i = 0; i < Quantity; i++)
            {
                if (FB.GetPinQuality(StartPin + i) != OpcQuality.Good)
                    return false;
            }
            return true;
        }
    }

}
