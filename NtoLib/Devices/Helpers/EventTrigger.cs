using System;

using FB;

namespace NtoLib.Devices.Helpers;

[Serializable]
public class EventTrigger
{
	private readonly FBDesignBase _owner;
	private readonly int _eventId;
	private readonly string _eventMessage;
	private readonly bool _autoDeactivate;
	private readonly TimeSpan _initialInactivityInterval;
	private readonly DateTime _startTime;

	private bool _isInitialInactivityPeriod = true;
	private bool _previousState;


	public EventTrigger(FBDesignBase owner, int eventId, string eventMessage, bool autoDeactivate = false)
		: this(owner, eventId, eventMessage, TimeSpan.Zero, autoDeactivate)
	{
	}

	public EventTrigger(FBDesignBase owner, int eventId, string eventMessage, TimeSpan initialInactivity,
		bool autoDeactivate = false)
	{
		_owner = owner;
		_eventId = eventId;
		_eventMessage = eventMessage;
		_autoDeactivate = autoDeactivate;
		_startTime = DateTime.Now;
		_initialInactivityInterval = initialInactivity;
	}


	public void Update(bool currentState)
	{
		if (IsInInitialInactivityPeriod())
		{
			return;
		}

		ProcessStateChange(currentState);
		_previousState = currentState;
	}

	private bool IsInInitialInactivityPeriod()
	{
		if (!_isInitialInactivityPeriod)
		{
			return false;
		}

		if (DateTime.Now.Subtract(_startTime) <= _initialInactivityInterval)
		{
			return true;
		}

		_isInitialInactivityPeriod = false;
		return false;
	}

	private void ProcessStateChange(bool currentState)
	{
		if (currentState && !_previousState)
		{
			ActivateEvent();
		}
		else if (!currentState && _previousState)
		{
			DeactivateEvent();
		}
	}

	private void ActivateEvent()
	{
		_owner.SetEventState(_eventId, true, _eventMessage);

		if (_autoDeactivate)
		{
			DeactivateEvent();
		}
	}

	private void DeactivateEvent()
	{
		_owner.SetEventState(_eventId, false);
	}
}
