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

        private bool _autoDeactivade;
        private bool _previousValue;



        public EventTrigger(FBDesignBase owner, int id, string message, bool autoDeactivate = false)
        {
            _owner = owner;

            _id = id;
            _message = message;

            _autoDeactivade = autoDeactivate;
        }



        public void Update(bool value)
        {
            if(value && !_previousValue)
            {
                _owner.SetEventState(_id, true, _message);

                if(_autoDeactivade)
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
