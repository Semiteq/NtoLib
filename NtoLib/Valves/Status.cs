namespace NtoLib.Valves
{
    internal struct Status
    {
        public State State;

        public bool Error;
        public bool BlockOpening;
        public bool BlockClosing;
    }
}
