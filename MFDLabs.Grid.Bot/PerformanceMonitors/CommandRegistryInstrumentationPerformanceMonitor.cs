using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class CommandRegistryInstrumentationPerformanceMonitor
    {
        private const string _Category = "MFDLabs.Grid.PerfmonV2";

        internal IRawValueCounter CommandsPerSecond { get; }
        internal IRawValueCounter FailedCommandsPerSecond { get; }
        internal IRawValueCounter NewThreadCountersPerSecond { get; }
        internal IRawValueCounter SucceededCommandsPerSecond { get; }
        internal IRawValueCounter NotFoundCommandsThatToldTheFrontendUser { get; }
        internal IRawValueCounter NotFoundCommandsThatDidNotTellTheFrontendUser { get; }
        internal IRawValueCounter CommandsThatExist { get; }
        internal IRawValueCounter CommandsThatAreDisabled { get; }
        internal IRawValueCounter DisabledCommandsThatAllowedAdminBypass { get; }
        internal IRawValueCounter DisabledCommandsThatDidNotAllowAdminBypass { get; }
        internal IRawValueCounter DisabledCommandsThatDidNotAllowBypass { get; }
        internal IRawValueCounter DisabledCommandsThatWereInvokedToTheFrontendUser { get; }
        internal IRawValueCounter CommandsThatAreEnabled { get; }
        internal IRawValueCounter CommandsThatTryToExecuteInNewThread { get; }
        internal IRawValueCounter NewThreadCommandsThatAreOnlyAvailableToAdmins { get; }
        internal IRawValueCounter NewThreadCommandsThatDidNotPassAdministratorCheck { get; }
        internal IRawValueCounter NewThreadCommandsThatPassedAdministratorCheck { get; }
        internal IRawValueCounter NewThreadCommandsThatWereAllowedToExecute { get; }
        internal IRawValueCounter NewThreadCommandsThatWereNotAllowedToExecute { get; }
        internal IRawValueCounter CommandsThatDidNotTryNewThreadExecution { get; }
        internal IRawValueCounter CommandsThatPassedAllChecks { get; }
        internal IRawValueCounter CommandsNotExecutedInNewThread { get; }
        internal IRawValueCounter FailedCommandsThatTimedOut { get; }
        internal IRawValueCounter FailedCommandsThatTriedToAccessOfflineGridServer { get; }
        internal IRawValueCounter FailedCommandsThatTriggeredAFaultException { get; }
        internal IRawValueCounter FailedFaultCommandsThatWereDuplicateInvocations { get; }
        internal IRawValueCounter FailedFaultCommandsThatWereNotDuplicateInvocations { get; }
        internal IRawValueCounter FailedFaultCommandsThatWereLuaExceptions { get; }
        internal IRawValueCounter FailedCommandsThatWereUnknownExceptions { get; }
        internal IRawValueCounter FailedCommandsThatLeakedExceptionInfo { get; }
        internal IRawValueCounter FailedCommandsThatWerePublicallyMasked { get; }
        internal IRawValueCounter CommandsParsedAndInsertedIntoRegistry { get; }
        internal IRawValueCounter CommandNamespacesThatHadNoClasses { get; }
        internal IRawValueCounter CommandsInNamespaceThatWereNotClasses { get; }
        internal IRawValueCounter CommandThatWereNotStateSpecific { get; }
        internal IRawValueCounter StateSpecificCommandsThatHadNoAliases { get; }
        internal IRawValueCounter StateSpecificCommandsThatHadNoName { get; }
        internal IRawValueCounter StateSpecificCommandsThatHadNoNullButEmptyDescription { get; }
        internal IRawValueCounter StateSpecificCommandsThatWereAddedToTheRegistry { get; }
        internal IRawValueCounter CommandRegistryRegistrationsThatFailed { get; }
        internal IRawValueCounter CommandsThatDidNotExist { get; }
        internal IRawValueCounter CommandsThatFinished { get; }
        internal IRawValueCounter NewThreadCommandsThatFinished { get; }
        internal IRawValueCounter NewThreadCommandsThatPassedChecks { get; }
        internal IAverageValueCounter AverageRequestTime { get; }
        internal IAverageValueCounter AverageThreadRequestTime { get; }

        internal CommandRegistryInstrumentationPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null)
            {
                throw new ArgumentNullException("counterRegistry");
            }

            var instance = $"{SystemGlobal.Singleton.GetMachineID()} ({SystemGlobal.Singleton.GetMachineHost()})";

            CommandsPerSecond = counterRegistry.GetRawValueCounter(_Category, "CommandsPerSecond", instance);
            FailedCommandsPerSecond = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsPerSecond", instance);
            NewThreadCountersPerSecond = counterRegistry.GetRawValueCounter(_Category, "NewThreadCountersPerSecond", instance);
            SucceededCommandsPerSecond = counterRegistry.GetRawValueCounter(_Category, "SucceededCommandsPerSecond", instance);
            NotFoundCommandsThatToldTheFrontendUser = counterRegistry.GetRawValueCounter(_Category, "NotFoundCommandsThatToldTheFrontendUser", instance);
            NotFoundCommandsThatDidNotTellTheFrontendUser = counterRegistry.GetRawValueCounter(_Category, "NotFoundCommandsThatDidNotTellTheFrontendUser", instance);
            CommandsThatExist = counterRegistry.GetRawValueCounter(_Category, "CommandsThatExist", instance);
            CommandsThatAreDisabled = counterRegistry.GetRawValueCounter(_Category, "CommandsThatAreDisabled", instance);
            DisabledCommandsThatAllowedAdminBypass = counterRegistry.GetRawValueCounter(_Category, "DisabledCommandsThatAllowedAdminBypass", instance);
            DisabledCommandsThatDidNotAllowAdminBypass = counterRegistry.GetRawValueCounter(_Category, "DisabledCommandsThatDidNotAllowAdminBypass", instance);
            DisabledCommandsThatDidNotAllowBypass = counterRegistry.GetRawValueCounter(_Category, "DisabledCommandsThatDidNotAllowBypass", instance);
            DisabledCommandsThatWereInvokedToTheFrontendUser = counterRegistry.GetRawValueCounter(_Category, "DisabledCommandsThatWereInvokedToTheFrontendUser", instance);
            CommandsThatAreEnabled = counterRegistry.GetRawValueCounter(_Category, "CommandsThatAreEnabled", instance);
            CommandsThatTryToExecuteInNewThread = counterRegistry.GetRawValueCounter(_Category, "CommandsThatTryToExecuteInNewThread", instance);
            NewThreadCommandsThatAreOnlyAvailableToAdmins = counterRegistry.GetRawValueCounter(_Category, "NewThreadCommandsThatAreOnlyAvailableToAdmins", instance);
            NewThreadCommandsThatDidNotPassAdministratorCheck = counterRegistry.GetRawValueCounter(_Category, "NewThreadCommandsThatDidNotPassAdministratorCheck", instance);
            NewThreadCommandsThatPassedAdministratorCheck = counterRegistry.GetRawValueCounter(_Category, "NewThreadCommandsThatPassedAdministratorCheck", instance);
            NewThreadCommandsThatWereAllowedToExecute = counterRegistry.GetRawValueCounter(_Category, "NewThreadCommandsThatWereAllowedToExecute", instance);
            NewThreadCommandsThatWereNotAllowedToExecute = counterRegistry.GetRawValueCounter(_Category, "NewThreadCommandsThatWereNotAllowedToExecute", instance);
            CommandsThatDidNotTryNewThreadExecution = counterRegistry.GetRawValueCounter(_Category, "CommandsThatDidNotTryNewThreadExecution", instance);
            CommandsThatPassedAllChecks = counterRegistry.GetRawValueCounter(_Category, "CommandsThatPassedAllChecks", instance);
            CommandsNotExecutedInNewThread = counterRegistry.GetRawValueCounter(_Category, "CommandsNotExecutedInNewThread", instance);
            FailedCommandsThatTimedOut = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsThatTimedOut", instance);
            FailedCommandsThatTriedToAccessOfflineGridServer = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsThatTriedToAccessOfflineGridServer", instance);
            FailedCommandsThatTriggeredAFaultException = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsThatTriggeredAFaultException", instance);
            FailedFaultCommandsThatWereDuplicateInvocations = counterRegistry.GetRawValueCounter(_Category, "FailedFaultCommandsThatWereDuplicateInvocations", instance);
            FailedFaultCommandsThatWereNotDuplicateInvocations = counterRegistry.GetRawValueCounter(_Category, "FailedFaultCommandsThatWereNotDuplicateInvocations", instance);
            FailedFaultCommandsThatWereLuaExceptions = counterRegistry.GetRawValueCounter(_Category, "FailedFaultCommandsThatWereLuaExceptions", instance);
            FailedCommandsPerSecond = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsThatWereUnknownExceptions", instance);
            FailedCommandsThatWereUnknownExceptions = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsPerSecond", instance);
            FailedCommandsThatLeakedExceptionInfo = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsThatLeakedExceptionInfo", instance);
            FailedCommandsThatWerePublicallyMasked = counterRegistry.GetRawValueCounter(_Category, "FailedCommandsThatWerePublicallyMasked", instance);
            CommandsParsedAndInsertedIntoRegistry = counterRegistry.GetRawValueCounter(_Category, "CommandsParsedAndInsertedIntoRegistry", instance);
            CommandNamespacesThatHadNoClasses = counterRegistry.GetRawValueCounter(_Category, "CommandNamespacesThatHadNoClasses", instance);
            CommandsInNamespaceThatWereNotClasses = counterRegistry.GetRawValueCounter(_Category, "CommandsInNamespaceThatWereNotClasses", instance);
            CommandThatWereNotStateSpecific = counterRegistry.GetRawValueCounter(_Category, "CommandThatWereNotStateSpecific", instance);
            StateSpecificCommandsThatHadNoAliases = counterRegistry.GetRawValueCounter(_Category, "StateSpecificCommandsThatHadNoAliases", instance);
            StateSpecificCommandsThatHadNoName = counterRegistry.GetRawValueCounter(_Category, "StateSpecificCommandsThatHadNoName", instance);
            StateSpecificCommandsThatHadNoNullButEmptyDescription = counterRegistry.GetRawValueCounter(_Category, "StateSpecificCommandsThatHadNoNullButEmptyDescription", instance);
            StateSpecificCommandsThatWereAddedToTheRegistry = counterRegistry.GetRawValueCounter(_Category, "StateSpecificCommandsThatWereAddedToTheRegistry", instance);
            CommandRegistryRegistrationsThatFailed = counterRegistry.GetRawValueCounter(_Category, "CommandRegistryRegistrationsThatFailed", instance);
            CommandsThatDidNotExist = counterRegistry.GetRawValueCounter(_Category, "CommandsThatDidNotExist", instance);
            CommandsThatFinished = counterRegistry.GetRawValueCounter(_Category, "CommandsThatFinished", instance);
            NewThreadCommandsThatFinished = counterRegistry.GetRawValueCounter(_Category, "NewThreadCommandsThatFinished", instance);
            NewThreadCommandsThatPassedChecks = counterRegistry.GetRawValueCounter(_Category, "NewThreadCommandsThatPassedChecks", instance);
            AverageRequestTime = counterRegistry.GetAverageValueCounter(_Category, "AverageRequestTime", instance);
            AverageThreadRequestTime = counterRegistry.GetAverageValueCounter(_Category, "AverageThreadRequestTime", instance);
        }
    }
}
