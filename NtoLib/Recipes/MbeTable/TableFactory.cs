using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable;

namespace NtoLib.Recipes.MbeTable
{
    internal class TableFactory
    {
        public TableFactory() { }

        private List<TableColumn> ColumnHeaders(string ActionName)
        {
            List<TableColumn> headers = new()
            {
                new("Действие", Actions),
                //new("Номер", numberEnum),
                new("Задание", CellType._float),
                new("Скорость/Время", CellType._float),
                new("Время", CellType._float),
                new("Комментарий", CellType._string)
            };
        return headers;
        }

        public DataGridViewRow RecipeLine()
        {
            DataGridViewRow Row = new();
            return Row;
        }

        public List<TableColumn> ColumnHeaders()
        {
            List<TableColumn> Row = new();
            return Row;
        }

        //public List<TCell> Cells(params object[] value) - любая длина
        public List<TCell> Cells()
        {
            List<TCell> Row = new();
            return Row;
        }

        public static TableEnumType Actions = new()
        {
            // TODO: переделать! Убрать постоянную реинициализацию!

            { Commands.CLOSE,            10 }, //shutter
            { Commands.OPEN,             20 }, //shutter
            { Commands.OPEN_TIME,        30 }, //shutter
            { Commands.CLOSE_ALL,        40 }, //shutter

            { Commands.TEMP,             50 }, //heater
            { Commands.TEMP_WAIT,        60 }, //heater
            { Commands.TEMP_BY_SPEED,    70 }, //heater
            { Commands.TEMP_BY_TIME,     80 }, //heater
            { Commands.POWER,            90 }, //heater
            { Commands.POWER_WAIT,       100 },//heater
            { Commands.POWER_BY_SPEED,   110 },//heater
            { Commands.POWER_BY_TIME,    120 },//heater

            { Commands.WAIT,             130 },
            { Commands.FOR,              140 },
            { Commands.END_FOR,          150 },
            { Commands.PAUSE,            160 },
            { Commands.NH3_OPEN,         170 },//not implemented
            { Commands.NH3_CLOSE,        180 },//not implemented
            { Commands.NH3_PURGE,        190 } //not implemented
        };
    }
}
