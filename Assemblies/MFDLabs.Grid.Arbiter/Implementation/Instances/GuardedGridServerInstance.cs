/*

    File name: GuardedGridServerInstance.cs
    Written By: @networking-owk
    Description: Represents the sentinel class for a grid server instance to be consumed by the arbiter.

    Copyright MFDLABS 2001-2022. All rights reserved.

*/

/*using System;
using System.ServiceModel;
using MFDLabs.Sentinels;
using MFDLabs.Threading;

using uint32 = System.UInt32;

namespace MFDLabs.Grid;

public partial class GridServerArbiter
{
    public class GuardedGridServerInstance : GridServerInstance, IDisposable
    {

        #region |Constants|

        private const string JobAlreadyRunningException = "Cannot invoke BatchJob while another job is running";
        private const string BatchJobTimeout = "BatchJob Timeout";

        #endregion |Constants|

        #region |Private Members|

        private static bool TripReasonAuthority(Exception ex)
        {
            if (ex is FaultException fault)
            {
                // If ex is a fault exception and is complaining about
                // batch job errors, jobs timing out

                return fault.Message switch
                {
                    JobAlreadyRunningException or BatchJobTimeout => true,
                    _ => false,
                };
            }

            return true;
        }

        private TimeSpan RetryIntervalCalculator()
        {
            var failure = NumberOfFailures;

            return ExponentialBackoff.CalculateBackoff(
                failure,
                _failureThreshold,
                _baseRetryInterval,
                _maxRetryInterval,
                Jitter.Equal
            );
        }

        private readonly ExecutionCircuitBreaker _circuitBreaker;
        private readonly uint32 _failureThreshold;
        private readonly TimeSpan _baseRetryInterval;
        private readonly TimeSpan _maxRetryInterval;

        private Atomic<uint32> _numberOfFailures;


        #endregion |Private Members|

        #region |Informative Members|

        public static TimeSpan DefaultLease => global::MFDLabs.Grid.Properties.Settings.Default.DefaultLeasedGridServerInstanceLease;

        public uint32 FailureThreshold => _failureThreshold;
        public uint32 NumberOfFailures => _numberOfFailures;
        public bool IsTripped => _circuitBreaker.IsTripped;
        public new bool IsAvailable => base.IsAvailable && !IsTripped;

        #endregion |Informative Members|
    }
}*/