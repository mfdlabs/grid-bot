using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class CommandRegistryInstrumentationPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.PerfmonV2";

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
                throw new ArgumentNullException(nameof(counterRegistry));
            }

            var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

            CommandsPerSecond = counterRegistry.GetRawValueCounter(Category, "CommandsPerSecond", instance);
            FailedCommandsPerSecond = counterRegistry.GetRawValueCounter(Category, "FailedCommandsPerSecond", instance);
            NewThreadCountersPerSecond = counterRegistry.GetRawValueCounter(Category, "NewThreadCountersPerSecond", instance);
            SucceededCommandsPerSecond = counterRegistry.GetRawValueCounter(Category, "SucceededCommandsPerSecond", instance);
            NotFoundCommandsThatToldTheFrontendUser = counterRegistry.GetRawValueCounter(Category, "NotFoundCommandsThatToldTheFrontendUser", instance);
            NotFoundCommandsThatDidNotTellTheFrontendUser = counterRegistry.GetRawValueCounter(Category, "NotFoundCommandsThatDidNotTellTheFrontendUser", instance);
            CommandsThatExist = counterRegistry.GetRawValueCounter(Category, "CommandsThatExist", instance);
            CommandsThatAreDisabled = counterRegistry.GetRawValueCounter(Category, "CommandsThatAreDisabled", instance);
            DisabledCommandsThatAllowedAdminBypass = counterRegistry.GetRawValueCounter(Category, "DisabledCommandsThatAllowedAdminBypass", instance);
            DisabledCommandsThatDidNotAllowAdminBypass = counterRegistry.GetRawValueCounter(Category, "DisabledCommandsThatDidNotAllowAdminBypass", instance);
            DisabledCommandsThatDidNotAllowBypass = counterRegistry.GetRawValueCounter(Category, "DisabledCommandsThatDidNotAllowBypass", instance);
            DisabledCommandsThatWereInvokedToTheFrontendUser = counterRegistry.GetRawValueCounter(Category, "DisabledCommandsThatWereInvokedToTheFrontendUser", instance);
            CommandsThatAreEnabled = counterRegistry.GetRawValueCounter(Category, "CommandsThatAreEnabled", instance);
            CommandsThatTryToExecuteInNewThread = counterRegistry.GetRawValueCounter(Category, "CommandsThatTryToExecuteInNewThread", instance);
            NewThreadCommandsThatAreOnlyAvailableToAdmins = counterRegistry.GetRawValueCounter(Category, "NewThreadCommandsThatAreOnlyAvailableToAdmins", instance);
            NewThreadCommandsThatDidNotPassAdministratorCheck = counterRegistry.GetRawValueCounter(Category, "NewThreadCommandsThatDidNotPassAdministratorCheck", instance);
            NewThreadCommandsThatPassedAdministratorCheck = counterRegistry.GetRawValueCounter(Category, "NewThreadCommandsThatPassedAdministratorCheck", instance);
            NewThreadCommandsThatWereAllowedToExecute = counterRegistry.GetRawValueCounter(Category, "NewThreadCommandsThatWereAllowedToExecute", instance);
            NewThreadCommandsThatWereNotAllowedToExecute = counterRegistry.GetRawValueCounter(Category, "NewThreadCommandsThatWereNotAllowedToExecute", instance);
            CommandsThatDidNotTryNewThreadExecution = counterRegistry.GetRawValueCounter(Category, "CommandsThatDidNotTryNewThreadExecution", instance);
            CommandsThatPassedAllChecks = counterRegistry.GetRawValueCounter(Category, "CommandsThatPassedAllChecks", instance);
            CommandsNotExecutedInNewThread = counterRegistry.GetRawValueCounter(Category, "CommandsNotExecutedInNewThread", instance);
            FailedCommandsThatTimedOut = counterRegistry.GetRawValueCounter(Category, "FailedCommandsThatTimedOut", instance);
            FailedCommandsThatTriedToAccessOfflineGridServer = counterRegistry.GetRawValueCounter(Category, "FailedCommandsThatTriedToAccessOfflineGridServer", instance);
            FailedCommandsThatTriggeredAFaultException = counterRegistry.GetRawValueCounter(Category, "FailedCommandsThatTriggeredAFaultException", instance);
            FailedFaultCommandsThatWereDuplicateInvocations = counterRegistry.GetRawValueCounter(Category, "FailedFaultCommandsThatWereDuplicateInvocations", instance);
            FailedFaultCommandsThatWereNotDuplicateInvocations = counterRegistry.GetRawValueCounter(Category, "FailedFaultCommandsThatWereNotDuplicateInvocations", instance);
            FailedFaultCommandsThatWereLuaExceptions = counterRegistry.GetRawValueCounter(Category, "FailedFaultCommandsThatWereLuaExceptions", instance);
            FailedCommandsPerSecond = counterRegistry.GetRawValueCounter(Category, "FailedCommandsThatWereUnknownExceptions", instance);
            FailedCommandsThatWereUnknownExceptions = counterRegistry.GetRawValueCounter(Category, "FailedCommandsPerSecond", instance);
            FailedCommandsThatLeakedExceptionInfo = counterRegistry.GetRawValueCounter(Category, "FailedCommandsThatLeakedExceptionInfo", instance);
            FailedCommandsThatWerePublicallyMasked = counterRegistry.GetRawValueCounter(Category, "FailedCommandsThatWerePublicallyMasked", instance);
            CommandsParsedAndInsertedIntoRegistry = counterRegistry.GetRawValueCounter(Category, "CommandsParsedAndInsertedIntoRegistry", instance);
            CommandNamespacesThatHadNoClasses = counterRegistry.GetRawValueCounter(Category, "CommandNamespacesThatHadNoClasses", instance);
            CommandsInNamespaceThatWereNotClasses = counterRegistry.GetRawValueCounter(Category, "CommandsInNamespaceThatWereNotClasses", instance);
            CommandThatWereNotStateSpecific = counterRegistry.GetRawValueCounter(Category, "CommandThatWereNotStateSpecific", instance);
            StateSpecificCommandsThatHadNoAliases = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandsThatHadNoAliases", instance);
            StateSpecificCommandsThatHadNoName = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandsThatHadNoName", instance);
            StateSpecificCommandsThatHadNoNullButEmptyDescription = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandsThatHadNoNullButEmptyDescription", instance);
            StateSpecificCommandsThatWereAddedToTheRegistry = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandsThatWereAddedToTheRegistry", instance);
            CommandRegistryRegistrationsThatFailed = counterRegistry.GetRawValueCounter(Category, "CommandRegistryRegistrationsThatFailed", instance);
            CommandsThatDidNotExist = counterRegistry.GetRawValueCounter(Category, "CommandsThatDidNotExist", instance);
            CommandsThatFinished = counterRegistry.GetRawValueCounter(Category, "CommandsThatFinished", instance);
            NewThreadCommandsThatFinished = counterRegistry.GetRawValueCounter(Category, "NewThreadCommandsThatFinished", instance);
            NewThreadCommandsThatPassedChecks = counterRegistry.GetRawValueCounter(Category, "NewThreadCommandsThatPassedChecks", instance);
            AverageRequestTime = counterRegistry.GetAverageValueCounter(Category, "AverageRequestTime", instance);
            AverageThreadRequestTime = counterRegistry.GetAverageValueCounter(Category, "AverageThreadRequestTime", instance);
        }
    }
}
