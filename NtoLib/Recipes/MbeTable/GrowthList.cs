using System;

namespace NtoLib.Recipes.MbeTable
{
    internal class GrowthList
    {
        public static TableEnumType ShutterNames { get; private set; }
        public static TableEnumType HeaterNames { get; private set; }
        public static TableEnumType CombinedList => HeaterNames + ShutterNames;

        public GrowthList()
        {
            MbeTableFB table = new();

            ShutterNames = FillDictNames(table, Params.FirstPinShutterName, Params.ShutterNameQuantity);
            HeaterNames = FillDictNames(table, Params.FirstPinHeaterName, Params.HeaterNameQuantity);
        }

        private TableEnumType FillDictNames(MbeTableFB table, int startBit, int quantity)
        {
            TableEnumType names = new();

            for (int i = 0; i < quantity; i++)
            {
                var pinString = table.GetPinString(i + startBit);
                if (pinString is not null)
                {
                    names.Add(pinString.ToString(), i);
                }
                else
                {
                    continue;
                }
            }
            return names;
        }
    }
}
