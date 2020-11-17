using System;
using System.Threading;

namespace CustomReadWriteLock
{
    public class CustomReadWrireLock
    {
        private int _reader = 0;
        private int _writer = 0;
        private readonly object _lockerR;
        private readonly object _lockerW;
        private readonly SpinWait _spinWait;

        public CustomReadWrireLock()
        {
            _lockerR = new Object();
            _lockerW = new Object();
            _spinWait = new SpinWait();
        }
        public void EnterRead()
        {
            lock (_lockerR)
            {
                while (Thread.VolatileRead(ref _writer) > 0)
                {
                    _spinWait.SpinOnce();
                    Monitor.Wait(_lockerR);
                }

                Interlocked.Increment(ref _reader);
            }
        }

        public void ExitRead()
        {
            lock (_lockerR)
            {
                Interlocked.Decrement(ref _reader);
                Monitor.PulseAll(_lockerR);
            }

            lock (_lockerW)
            {
                if (Thread.VolatileRead(ref _reader) == 0)
                    Monitor.PulseAll(_lockerW);
            }
        }

        public void EnterWrite()
        {
            lock (_lockerW)
            {
                lock (_lockerR)
                {
                    if (Thread.VolatileRead(ref _writer) == 0)
                        Monitor.PulseAll(_lockerR);

                    while (Thread.VolatileRead(ref _reader) != 0)
                    {
                        _spinWait.SpinOnce();
                        Monitor.Wait(_lockerR);
                    }
                }
                while (Thread.VolatileRead(ref _writer) > 0)
                {
                    _spinWait.SpinOnce();
                    Monitor.Wait(_lockerW);
                }

                Interlocked.Increment(ref _writer);
            }
        }
        public void ExitWrite()
        {
            lock (_lockerW)
            {
                Interlocked.Decrement(ref _writer);
                Monitor.PulseAll(_lockerW);
            }
        }
    }
}
