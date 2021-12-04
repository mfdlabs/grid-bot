// TODO: Base abstract class in here for system and user role sets
// persist data in Vault, or check the deployment type?

using System;

namespace MFDLabs.Discord.RoleSets
{
    public abstract class BaseRole : IDisposable
    {

        #region IDisposable Members

        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            _disposed = true;

            Dispose(disposing: true);
        }
        protected virtual void Dispose(bool disposing) {}

        private bool _disposed;

        #endregion IDisposable Members
    }
}
