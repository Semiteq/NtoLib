using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Close : RecipeLine
    {
        public const string ActionName = Commands.CLOSE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Close(int number = 1, string comment = "") : base(ActionName)
        {
            shutterName = GrowthList.ShutterNames.GetValueByIndex(number);
            int actionNumber = Actions[ActionName];
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, shutterName, number),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}