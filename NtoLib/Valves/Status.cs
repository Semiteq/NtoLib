namespace NtoLib.Valves
{
    public struct Status
    {
        public State State;

        public bool UsedByAutoMode;
        public bool BlockOpening;
        public bool BlockClosing;
        public bool ConnectionOk;
        public bool NotOpened;
        public bool NotClosed;
        public bool Collision;

        public bool AnyError => !ConnectionOk || NotOpened || NotClosed || Collision;
    }
}
