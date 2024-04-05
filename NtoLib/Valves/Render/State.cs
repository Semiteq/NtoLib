namespace NtoLib.Valves.Render
{
    internal struct State
    {
        public bool ConnectionOk;
        public bool Opened;
        public bool Closed;
        public bool Error;
        public bool OldError;
        public bool BlockOpening;
        public bool BlockClosing;
        public bool AutoMode;
        public bool Collision;
    }
}
