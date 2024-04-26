namespace NtoLib.Pumps
{
    public struct Status
    {
        public bool ConnectionOk;
        public bool MainError;
        public bool UsedByAutoMode;
        public bool WorkOnNominalSpeed;
        public bool Stopped;
        public bool Accelerating;
        public bool Decelerating;
        public bool Warning;
        public bool Message1;
        public bool Message2;
        public bool Message3;
        public bool Message4;
        public bool ForceStop;
        public bool BlockStart;
        public bool BlockStop;
        public bool Use;

        public bool AnimationNeeded => Accelerating || Decelerating;
    }
}
