using System;

namespace DOL.Network
{
    public sealed class SessionId : IDisposable
    {
        private readonly SessionIdAllocator _sessionIdAllocator;
        private ushort _value;
        private bool _isDisposed = false;

        public ushort Value => _value;

        public SessionId(SessionIdAllocator sessionIdAllocator)
        {
            _sessionIdAllocator = sessionIdAllocator;

            if (!_sessionIdAllocator.TryAllocate(out _value))
                throw new InvalidOperationException("No available session IDs.");

            if (_value == 0)
                throw new InvalidOperationException("Allocated session ID is out of range (0).");
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _sessionIdAllocator.Free(_value);
            _value = 0;
            _isDisposed = true;
        }
    }
}
