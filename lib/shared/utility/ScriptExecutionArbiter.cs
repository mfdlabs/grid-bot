namespace Grid.Bot.Utility;

using System.ServiceModel;

using Logging;
using Instrumentation;

/// <summary>
/// Provider for the <see cref="IGridServerArbiter"/> used by script executions.
/// </summary>
public static class ScriptExecutionArbiter
{
    /// <summary>
    /// The <see cref="IGridServerArbiter"/>
    /// </summary>
    public static IGridServerArbiter Singleton = new GridServerArbiter(
        ArbiterSettings.Singleton,
        StaticCounterRegistry.Instance,
        Logger.Singleton,
        new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            SendTimeout = ArbiterSettings.Singleton.ScriptExecutionArbiterMaxTimeout
        }
    );
}
