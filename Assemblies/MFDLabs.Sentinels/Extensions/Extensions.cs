using System;

namespace MFDLabs.Sentinels
{
    internal static class Extensions
    {
        internal static void CheckAndDispose(this IDisposable disposable) => disposable?.Dispose();
    }
}
