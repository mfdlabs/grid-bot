using System;
using Diagnostics;
using Instrumentation;

namespace Grid.Bot.PerformanceMonitors
{
    public sealed class CommandRegistryInstrumentationPerformanceMonitor
    {
        private const string Category = "Grid.PerfmonV2";

        public IRawValueCounter CommandsPerSecond { get; }
        public IRawValueCounter FailedCommandsPerSecond { get; }
        public IRawValueCounter SucceededCommandsPerSecond { get; }
        public IRawValueCounter NotFoundCommandsThatToldTheFrontendUser { get; }
        public IRawValueCounter NotFoundCommandsThatDidNotTellTheFrontendUser { get; }
        public IRawValueCounter CommandsThatExist { get; }
        public IRawValueCounter CommandsThatAreDisabled { get; }
        public IRawValueCounter DisabledCommandsThatAllowedAdminBypass { get; }
        public IRawValueCounter DisabledCommandsThatDidNotAllowAdminBypass { get; }
        public IRawValueCounter DisabledCommandsThatDidNotAllowBypass { get; }
        public IRawValueCounter DisabledCommandsThatWereInvokedToTheFrontendUser { get; }
        public IRawValueCounter CommandsThatAreEnabled { get; }
        public IRawValueCounter CommandsThatPassedAllChecks { get; }
        public IRawValueCounter FailedCommandsThatTimedOut { get; }
        public IRawValueCounter FailedCommandsThatTriedToAccessOfflineGridServer { get; }
        public IRawValueCounter FailedCommandsThatTriggeredAFaultException { get; }
        public IRawValueCounter FailedFaultCommandsThatWereDuplicateInvocations { get; }
        public IRawValueCounter FailedFaultCommandsThatWereNotDuplicateInvocations { get; }
        public IRawValueCounter FailedFaultCommandsThatWereLuaExceptions { get; }
        public IRawValueCounter FailedCommandsThatWereUnknownExceptions { get; }
        public IRawValueCounter FailedCommandsThatLeakedExceptionInfo { get; }
        public IRawValueCounter FailedCommandsThatWerePublicallyMasked { get; }
        public IRawValueCounter CommandsParsedAndInsertedIntoRegistry { get; }
        public IRawValueCounter CommandNamespacesThatHadNoClasses { get; }
        public IRawValueCounter CommandsInNamespaceThatWereNotClasses { get; }
        public IRawValueCounter CommandThatWereNotStateSpecific { get; }
        public IRawValueCounter StateSpecificCommandsThatHadNoAliases { get; }
        public IRawValueCounter StateSpecificCommandAliasesThatAlreadyExisted { get; }
        public IRawValueCounter StateSpecificCommandsThatHadNoName { get; }
        public IRawValueCounter StateSpecificCommandsThatHadNoNullButEmptyDescription { get; }
        public IRawValueCounter StateSpecificCommandsThatWereAddedToTheRegistry { get; }
        public IRawValueCounter CommandRegistryRegistrationsThatFailed { get; }
        public IRawValueCounter CommandsThatDidNotExist { get; }
        public IRawValueCounter CommandsThatFinished { get; }
        public IAverageValueCounter AverageRequestTime { get; }

        public CommandRegistryInstrumentationPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null)
            {
                throw new ArgumentNullException(nameof(counterRegistry));
            }

            var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

            CommandsPerSecond = counterRegistry.GetRawValueCounter(Category, "CommandsPerSecond", instance);
            FailedCommandsPerSecond = counterRegistry.GetRawValueCounter(Category, "FailedCommandsPerSecond", instance);;
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
            CommandsThatPassedAllChecks = counterRegistry.GetRawValueCounter(Category, "CommandsThatPassedAllChecks", instance);
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
            StateSpecificCommandAliasesThatAlreadyExisted = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandAliasesThatAlreadyExisted", instance);
            StateSpecificCommandsThatHadNoName = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandsThatHadNoName", instance);
            StateSpecificCommandsThatHadNoNullButEmptyDescription = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandsThatHadNoNullButEmptyDescription", instance);
            StateSpecificCommandsThatWereAddedToTheRegistry = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandsThatWereAddedToTheRegistry", instance);
            CommandRegistryRegistrationsThatFailed = counterRegistry.GetRawValueCounter(Category, "CommandRegistryRegistrationsThatFailed", instance);
            CommandsThatDidNotExist = counterRegistry.GetRawValueCounter(Category, "CommandsThatDidNotExist", instance);
            CommandsThatFinished = counterRegistry.GetRawValueCounter(Category, "CommandsThatFinished", instance);
            AverageRequestTime = counterRegistry.GetAverageValueCounter(Category, "AverageRequestTime", instance);
        }
    }
}
