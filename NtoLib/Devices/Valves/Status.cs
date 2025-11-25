namespace NtoLib.Devices.Valves
{
	public struct Status
	{
		public bool ConnectionOk;
		public bool NotOpened;
		public bool NotClosed;
		public bool UnknownState;
		public bool Collision;
		public bool UsedByAutoMode;
		public bool Opened;
		public bool OpenedSmoothly;
		public bool Closed;
		public bool OpeningClosing;

		public bool ForceClose;
		public bool BlockClosing;
		public bool BlockOpening;

		public bool AnyError => !ConnectionOk || NotOpened || NotClosed || Collision || UnknownState;

		public bool AnimationNeeded => OpeningClosing || (Collision && !OpenedSmoothly) || AnyError;
	}
}
