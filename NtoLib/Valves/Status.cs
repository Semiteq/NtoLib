namespace NtoLib.Valves
{
    internal struct Status
    {
        public State State;

        public bool UsedByAutoMode;
        public bool BlockOpening;
        public bool BlockClosing;
        public bool ForceClose;
        public bool NoConnection;
        public bool NotOpened;
        public bool NotClosed;
        public bool Collision;

        public bool AnyError => NoConnection || NotOpened || NotClosed || Collision;
    }
}
