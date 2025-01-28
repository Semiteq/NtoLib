using FB.VisualFB;
using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable
{
    public class ReadPins : VisualFBBase
    {
        public int StartPin { get; }
        public int Quantity { get; }

        // Необходимо передавать стейт в ФБ, без него ошибка Null reference 
        private VisualFBBase FB { get; }

        public ReadPins(int startPin, int quantity, VisualFBBase fb)
        {
            StartPin = startPin;
            Quantity = quantity;
            FB = fb;
        }

        public TableEnumType ReadPinNames()
        {
            TableEnumType names = new();

            for (int i = 0; i < Quantity; i++)
            {
                string pinString = FB.GetPinValue<string>(i + StartPin);
                if (!string.IsNullOrEmpty(pinString))
                {
                    names.Add(pinString, i + StartPin);
                }
            }

            return names;
        }

        public bool IsPinGroupQualityGood()
        {
            for (int i = 0; i < Quantity; i++)
            {
                if (FB.GetPinQuality(StartPin + i) != OpcQuality.Good)
                    return false;
            }
            return true;
        }
    }

}
