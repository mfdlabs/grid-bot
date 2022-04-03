/* Copyright MFDLABS Corporation. All rights reserved. */

using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public sealed class CommandRegistryInstrumentationPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.PerfmonV2";

        public IRawValueCounter CommandsPerSecond { get; }
        public IRawValueCounter FailedCommandsPerSecond { get; }
        public IRawValueCounter NewThreadCountersPerSecond { get; }
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
        public IRawValueCounter CommandsThatTryToExecuteInNewThread { get; }
        public IRawValueCounter NewThreadCommandsThatAreOnlyAvailableToAdmins { get; }
        public IRawValueCounter NewThreadCommandsThatDidNotPassAdministratorCheck { get; }
        public IRawValueCounter NewThreadCommandsThatPassedAdministratorCheck { get; }
        public IRawValueCounter NewThreadCommandsThatWereAllowedToExecute { get; }
        public IRawValueCounter NewThreadCommandsThatWereNotAllowedToExecute { get; }
        public IRawValueCounter CommandsThatDidNotTryNewThreadExecution { get; }
        public IRawValueCounter CommandsThatPassedAllChecks { get; }
        public IRawValueCounter CommandsNotExecutedInNewThread { get; }
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
        public IRawValueCounter NewThreadCommandsThatFinished { get; }
        public IRawValueCounter NewThreadCommandsThatPassedChecks { get; }
        public IAverageValueCounter AverageRequestTime { get; }
        public IAverageValueCounter AverageThreadRequestTime { get; }

        public CommandRegistryInstrumentationPerformanceMonitor(ICounterRegistry counterRegistry)
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
            StateSpecificCommandAliasesThatAlreadyExisted = counterRegistry.GetRawValueCounter(Category, "StateSpecificCommandAliasesThatAlreadyExisted", instance);
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
