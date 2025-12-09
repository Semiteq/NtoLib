namespace NtoLib.Devices.Pumps;

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
	public bool SafeMode;
	public bool Units;
	public bool ForceStop;
	public bool BlockStart;
	public bool BlockStop;
	public bool Use;


	public bool AnyError =>
		MainError || !ConnectionOk || !(WorkOnNominalSpeed || Stopped || Accelerating || Decelerating);

	public bool AnimationNeeded => Accelerating || Decelerating || AnyError;


	public float Temperature;


	public float Speed;

	public float Voltage;
	public float Current;
	public float Power;

	public float TemperatureIn;
	public float TemperatureOut;
}
