using System.Diagnostics;

namespace MFDLabs.Abstractions
{
    /// <summary>
    /// Base singleton class, thread safe stuff.
    /// </summary>
    /// <typeparam name="TSingleton"></typeparam>
    public class SingletonBase<TSingleton>
        where TSingleton : class, new()
    {
        /// <summary>
        /// The singleton instance to be returned when indexing this singleton.
        /// The <see cref="TSingleton"/> parameter should have at least one public empty constructor.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static readonly TSingleton Singleton = new();
    }
}
