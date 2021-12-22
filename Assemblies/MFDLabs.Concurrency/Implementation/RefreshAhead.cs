using System;
using System.Threading;
using Microsoft.Ccr.Core;

// ReSharper disable once CheckNamespace
namespace MFDLabs.Concurrency
{
    /// <summary>
    /// Represents a CCR based refresh class.
    /// </summary>
    public class RefreshAhead<T> : IDisposable
    {
        private DateTime _lastRefresh = DateTime.MinValue;
        private readonly Timer _refreshTimer;
        private T _value;

        /// <summary>
        /// The last time the class refreshed
        /// </summary>
        public TimeSpan IntervalSinceRefresh => DateTime.Now.Subtract(_lastRefresh);

        /// <summary>
        /// The raw value.
        /// </summary>
        public T Value => _value;

        private RefreshAhead(T initialValue, TimeSpan refreshInterval, Func<T> refreshDelegate)
        {
            var refreshIntervalInMilliseconds = (int)refreshInterval.TotalMilliseconds;

            _value = initialValue;
            _lastRefresh = DateTime.Now;

            _refreshTimer = new Timer(
                (stateInfo) => Refresh(refreshDelegate),
                null,
                refreshIntervalInMilliseconds,
                refreshIntervalInMilliseconds
            );
        }
        private RefreshAhead(T initialValue, TimeSpan refreshInterval, Action<PortSet<T, Exception>> refreshDelegate)
        {
            var refreshIntervalInMilliseconds = (int)refreshInterval.TotalMilliseconds;

            _value = initialValue;
            _lastRefresh = DateTime.Now;

            _refreshTimer = new Timer(
                (stateInfo) => Refresh(refreshDelegate),
                null,
                refreshIntervalInMilliseconds,
                refreshIntervalInMilliseconds
            );
        }

        /// <summary>
        /// New CCR refresher
        /// </summary>
        /// <param name="refreshInterval"></param>
        /// <param name="refreshDelegate"></param>
        public RefreshAhead(TimeSpan refreshInterval, Func<T> refreshDelegate)
        {
            var refreshIntervalInMilliseconds = (int)refreshInterval.TotalMilliseconds;

            _refreshTimer = new Timer(
                (stateInfo) => Refresh(refreshDelegate),
                null,
                refreshIntervalInMilliseconds,
                refreshIntervalInMilliseconds
            );
        }

        private void Refresh(Func<T> refreshDelegate)
        {
            try
            {
                _value = refreshDelegate();
                _lastRefresh = DateTime.Now;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex);
            }
        }
        private void Refresh(Action<PortSet<T, Exception>> refreshDelegate)
        {
            try
            {
                var valueResult = new PortSet<T, Exception>();
                refreshDelegate(valueResult);
                ConcurrencyService.Singleton.Choice(
                    valueResult,
                    value =>
                    {
                        _value = value;
                        _lastRefresh = DateTime.Now;
                    }
                );
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex);
            }
        }

        /// <summary>
        /// Construct one :)
        /// </summary>
        /// <param name="refreshInterval"></param>
        /// <param name="refreshDelegate"></param>
        /// <returns></returns>
        public static RefreshAhead<T> ConstructAndPopulate(TimeSpan refreshInterval, Func<T> refreshDelegate)
        {
            var value = refreshDelegate();
            var refreshAhead = new RefreshAhead<T>(value, refreshInterval, refreshDelegate);
            return refreshAhead;
        }
        /// <summary>
        /// Construct one :)
        /// </summary>
        /// <param name="refreshInterval"></param>
        /// <param name="refreshDelegate"></param>
        /// <param name="result"></param>
        public static void ConstructAndPopulate(TimeSpan refreshInterval, Action<PortSet<T, Exception>> refreshDelegate, PortSet<RefreshAhead<T>, Exception> result)
        {
            var valueResult = new PortSet<T, Exception>();
            refreshDelegate(valueResult);
            ConcurrencyService.Singleton.Choice(
                valueResult,
                (value) =>
                {
                    var refreshAhead = new RefreshAhead<T>(value, refreshInterval, refreshDelegate);
                    result.Post(refreshAhead);
                },
                result.Post
            );
        }

        #region IDisposable Members
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_refreshTimer != null)
                _refreshTimer.Dispose();
        }

        #endregion
    }
}