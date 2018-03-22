using System;
using NStack;
using Terminal.Gui;

namespace loopdi
{
    public class Accelerator : Label
    {
        private readonly Key    _Trigger;
        private readonly Action _Action;

        public Accelerator( int x, int y, ustring text, Key trigger, Action action ) : base( x, y, text )
        {
            _Trigger = trigger;
            _Action  = action;
        }


        public override bool ProcessHotKey( KeyEvent keyEvent )
        {
            if (keyEvent.Key == _Trigger)
            {
                _Action();
            }
            return base.ProcessHotKey( keyEvent );
        }
    }
}