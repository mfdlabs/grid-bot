namespace Redis;

using System;
using System.Collections.Generic;

public partial class SelfHealingConnectionBuilder
{
    private partial class SelfHealingConnectionMultiplexer
    {
        private class BadStateRecorder
        {
            private readonly LinkedList<DateTime> _Occurrences = new();
            private readonly Func<DateTime> _GetCurrentTimeFunc;
            private readonly ISelfHealingConnectionMultiplexerSettings _Settings;

            private DateTime _LastReset;

            public long Version { get; private set; }

            public BadStateRecorder(ISelfHealingConnectionMultiplexerSettings settings, Func<DateTime> getCurrentTimeFunc)
            {
                _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
                _GetCurrentTimeFunc = getCurrentTimeFunc ?? throw new ArgumentNullException(nameof(getCurrentTimeFunc));
                _LastReset = DateTime.MinValue;
            }

            public event Action<long> BadStateDetected;

            public void Record()
            {
                var now = _GetCurrentTimeFunc();
                if (now < _LastReset + _Settings.ResetGracePeriod)
                    return;

                lock (_Occurrences)
                {
                    _Occurrences.AddLast(now);

                    while (true)
                    {
                        if (!(_Occurrences.First?.Value < now - _Settings.DetectionInterval))
                            break;

                        _Occurrences.RemoveFirst();
                    }

                    if (_Occurrences.Count >= _Settings.DetectionThreshold)
                        BadStateDetected?.Invoke(Version);
                }
            }

            public bool Reset(long version)
            {
                lock (_Occurrences)
                {
                    if (version == Version)
                    {
                        _Occurrences.Clear();

                        Version += 1;
                        _LastReset = _GetCurrentTimeFunc();

                        return true;
                    }
                }

                return false;
            }
        }
    }
}
