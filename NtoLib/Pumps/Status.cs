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
        public bool Error1;
        public bool Error2;
        public bool Error3;
        public bool Error4;
        public bool ForceStop;
        public bool BlockStart;
        public bool BlockStop;
        public bool Use;

        public bool AnyError => Use && (!ConnectionOk || MainError || Error1 || Error2 || Error3 || Error4);
        public bool AnimationNeeded => Accelerating || Decelerating;
    }
}
