using System;
using System.Reactive.Subjects;
using NStack;
using Terminal.Gui;

namespace loopdi
{
    public class Accelerator : Label, IObservable<KeyEvent>
    {
        private Subject<KeyEvent> _KeyEvent = new Subject<KeyEvent>();

        public Accelerator( int x, int y, ustring text) : base( x, y, text )
        {
        }

        public override bool ProcessColdKey(KeyEvent keyEvent)
        {
            return base.ProcessColdKey(keyEvent);
        }
        public override bool ProcessKey(KeyEvent keyEvent)
        {
            return base.ProcessKey(keyEvent);
        }
        public override bool ProcessHotKey( KeyEvent keyEvent )
        {
            _KeyEvent.OnNext(keyEvent);
            return base.ProcessHotKey( keyEvent );
        }

        public IDisposable Subscribe(IObserver<KeyEvent> observer)
        {
            return _KeyEvent.Subscribe(observer);
        }
    }
}