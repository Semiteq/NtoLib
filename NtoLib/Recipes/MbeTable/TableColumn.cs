namespace NtoLib.Recipes.MbeTable
{
    internal class TableColumn
    {
        private string _name;
        private CellType _type;
        private TableEnumType _enumType;
        private int _gridIndex;

        public TableColumn(string Name, CellType type)
        {
            this._name = Name;
            this._type = type;
        }

        public TableColumn(string Name, TableEnumType enum_type)
        {
            this._name = Name;
            this._type = CellType._enum;
            this._enumType = enum_type;
        }

        public string Name => this._name;

        public CellType type => this._type;

        public TableEnumType EnumType => this._enumType;

        public int GridIndex
        {
            get => this._gridIndex;
            set => this._gridIndex = value;
        }
    }
}