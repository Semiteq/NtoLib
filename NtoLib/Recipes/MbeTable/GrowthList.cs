using System;

namespace NtoLib.Recipes.MbeTable
{
    internal class GrowthList
    {
        //todo: static?
        public static TableEnumType ShutterNames { get; private set; }
        public static TableEnumType HeaterNames { get; private set; }

        public GrowthList()
        {
            MbeTableFB table = new();

            //ShutterNames = FillDictNames(table, Params.FirstPinShutterName, Params.ShutterNameQuantity);
            ShutterNames = new() 
            {
                { "Shut1", 1 },
                { "Shut2", 2 }
            };

            HeaterNames = new()
            {
                { "Heat1", 1 },
                { "Heat2", 2 }
            };
            //HeaterNames = FillDictNames(table, Params.FirstPinHeaterName, Params.HeaterNameQuantity);
        }

        public string GetShutterName(int number)
        {
            return ShutterNames.GetValueByIndex(number);
        }

        public string GetHeaterName(int number)
        {
            return HeaterNames.GetValueByIndex(number);
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
