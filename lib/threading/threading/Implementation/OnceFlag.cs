namespace Threading;

using System.Threading;

/// <summary>
/// Once flag to be used with Atomic based
/// once calling of methods.
///
/// Mimicks std::call_once
/// </summary>
public struct OnceFlag
{
    private const int NotCalled = 0, Called = 1;

    private int _called = NotCalled;

    /// <summary>
    /// Initialize a new instance of <see cref="OnceFlag"/>
    /// </summary>
    public OnceFlag() {}

    internal bool CheckAndSet() => Interlocked.CompareExchange(ref _called, Called, NotCalled) == NotCalled;
    internal void Reset() => Interlocked.CompareExchange(ref _called, 0, 1);
}
