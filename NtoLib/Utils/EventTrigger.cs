using FB;
using System;

namespace NtoLib.Utils
{
    [Serializable]
    public class EventTrigger
    {
        private FBDesignBase _owner;

        private int _id;
        private string _message;

        private bool _autoDeactivate;
        private bool _previousValue;

        private bool _initialInactivity = true;
        private DateTime _initialDataTime;
        private TimeSpan _initialInactivityInterval;



        public EventTrigger(FBDesignBase owner, int id, string message, bool autoDeactivate = false)
            : this(owner, id, message, TimeSpan.Zero, autoDeactivate)
        {

        }

        public EventTrigger(FBDesignBase owner, int id, string message, TimeSpan initialInactivity, bool autoDeactivate = false)
        {
            _owner = owner;

            _id = id;
            _message = message;

            _autoDeactivate = autoDeactivate;

            _initialDataTime = DateTime.Now;
            _initialInactivityInterval = initialInactivity;
        }



        public void Update(bool value)
        {
            if(_initialInactivity)
            {
                if(DateTime.Now.Subtract(_initialDataTime) > _initialInactivityInterval)
                    _initialInactivity = false;
                else
                    return;
            }


            if(value && !_previousValue)
            {
                _owner.SetEventState(_id, true, _message);

                if(_autoDeactivate)
                    _owner.SetEventState(_id, false);
            }
            else if(!value && _previousValue)
            {
                _owner.SetEventState(_id, false);
            }

            _previousValue = value;
        }
    }
}
