using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using MFDLabs.Grid.ComputeCloud;

namespace MFDLabs.Grid.Bot.Utility
{
    // This cannot follow the Singleton base pattern as it has a base class.
    // We could make a custom soap client that extends singleton base, but that is dumb.
    public sealed class SoapUtility : ComputeCloudServiceSoapClient
    {
        public static readonly SoapUtility Singleton = new SoapUtility(
            new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                SendTimeout = global::MFDLabs.Grid.Bot.Properties.Settings.Default.SoapUtilityRemoteServiceTimeout
            },
            new EndpointAddress("http://127.0.0.1:" + global::MFDLabs.Grid.Bot.Properties.Settings.Default.SoapUtilityRemoteServicePort)
        );

        private SoapUtility()
            : this("")
        {
        }

        public SoapUtility(string endpointConfigurationName)
            : base(endpointConfigurationName)
        {
        }

        public SoapUtility(string endpointConfigurationName, string remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        public SoapUtility(string endpointConfigurationName, EndpointAddress remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        public SoapUtility(Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {
        }

        public new string HelloWorld()
        {
            try
            {
                return base.HelloWorld();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return HelloWorld();
                }

                throw ex;
            }
        }

        public new async Task<string> HelloWorldAsync()
        {
            try
            {
                return await base.HelloWorldAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await HelloWorldAsync();
                }

                throw ex;
            }
        }

        public new string GetVersion()
        {
            try
            {
                return base.GetVersion();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return GetVersion();
                }

                throw ex;
            }
        }

        public new async Task<string> GetVersionAsync()
        {
            try
            {
                return await base.GetVersionAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await GetVersionAsync();
                }

                throw ex;
            }
        }

        public new Status GetStatus()
        {
            try
            {
                return base.GetStatus();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return GetStatus();
                }

                throw ex;
            }
        }

        public new async Task<Status> GetStatusAsync()
        {
            try
            {
                return await base.GetStatusAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await GetStatusAsync();
                }

                throw ex;
            }
        }

        public new LuaValue[] OpenJob(Job job, ScriptExecution script)
        {
            try
            {
                return base.OpenJob(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return OpenJob(job, script);
                }

                throw ex;
            }
        }

        public new async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.OpenJobAsync(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await OpenJobAsync(job, script);
                }

                throw ex;
            }
        }

        public new LuaValue[] OpenJobEx(Job job, ScriptExecution script)
        {
            try
            {
                return base.OpenJobEx(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return OpenJobEx(job, script);
                }

                throw ex;
            }
        }

        public new async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.OpenJobExAsync(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await OpenJobExAsync(job, script);
                }

                throw ex;
            }
        }

        public new double RenewLease(string jobID, double expirationInSeconds)
        {
            try
            {
                return base.RenewLease(jobID, expirationInSeconds);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return RenewLease(jobID, expirationInSeconds);
                }

                throw ex;
            }
        }

        public new async Task<double> RenewLeaseAsync(string jobID, double expirationInSeconds)
        {
            try
            {
                return await base.RenewLeaseAsync(jobID, expirationInSeconds);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await RenewLeaseAsync(jobID, expirationInSeconds);
                }
                throw ex;
            }
        }

        public new LuaValue[] Execute(string jobID, ScriptExecution script)
        {
            try
            {
                return base.Execute(jobID, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return Execute(jobID, script);
                }

                throw ex;
            }
        }

        public new async Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script)
        {
            try
            {
                return await base.ExecuteAsync(jobID, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await ExecuteAsync(jobID, script);
                }
                throw ex;
            }
        }

        public new LuaValue[] ExecuteEx(string jobID, ScriptExecution script)
        {
            try
            {
                return base.ExecuteEx(jobID, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return ExecuteEx(jobID, script);
                }

                throw ex;
            }
        }

        public new async Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script)
        {
            try
            {
                return await base.ExecuteExAsync(jobID, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await ExecuteExAsync(jobID, script);
                }
                throw ex;
            }
        }

        public new void CloseJob(string jobID)
        {
            try
            {
                base.CloseJob(jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    CloseJob(jobID);
                    return;
                }

                throw ex;
            }
        }

        public new async Task CloseJobAsync(string jobID)
        {
            try
            {
                await base.CloseJobAsync(jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    await CloseJobAsync(jobID); return;
                }
                throw ex;
            }
        }

        public new LuaValue[] BatchJob(Job job, ScriptExecution script)
        {
            try
            {
                return base.BatchJob(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return BatchJob(job, script);
                }

                throw ex;
            }
        }

        public new async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.BatchJobAsync(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await BatchJobAsync(job, script);
                }
                throw ex;
            }
        }

        public new LuaValue[] BatchJobEx(Job job, ScriptExecution script)
        {
            try
            {
                return base.BatchJobEx(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return BatchJobEx(job, script);
                }

                throw ex;
            }
        }

        public new async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.BatchJobExAsync(job, script);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await BatchJobExAsync(job, script);
                }
                throw ex;
            }
        }

        public new double GetExpiration(string jobID)
        {
            try
            {
                return base.GetExpiration(jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return GetExpiration(jobID);
                }

                throw ex;
            }
        }

        public new async Task<double> GetExpirationAsync(string jobID)
        {
            try
            {
                return await base.GetExpirationAsync(jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await GetExpirationAsync(jobID);
                }
                throw ex;
            }
        }

        public new Job[] GetAllJobs()
        {
            try
            {
                return base.GetAllJobs();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return GetAllJobs();
                }

                throw ex;
            }
        }

        public new async Task<GetAllJobsResponse> GetAllJobsAsync()
        {
            try
            {
                return await base.GetAllJobsAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await GetAllJobsAsync();
                }
                throw ex;
            }
        }

        public new Job[] GetAllJobsEx()
        {
            try
            {
                return base.GetAllJobsEx();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return GetAllJobsEx();
                }

                throw ex;
            }
        }

        public new async Task<Job[]> GetAllJobsExAsync()
        {
            try
            {
                return await base.GetAllJobsExAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await GetAllJobsExAsync();
                }
                throw ex;
            }
        }

        public new int CloseExpiredJobs()
        {
            try
            {
                return base.CloseExpiredJobs();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return CloseExpiredJobs();
                }

                throw ex;
            }
        }

        public new async Task<int> CloseExpiredJobsAsync()
        {
            try
            {
                return await base.CloseExpiredJobsAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await CloseExpiredJobsAsync();
                }
                throw ex;
            }
        }

        public new int CloseAllJobs()
        {
            try
            {
                return base.CloseAllJobs();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return CloseAllJobs();
                }

                throw ex;
            }
        }

        public new async Task<int> CloseAllJobsAsync()
        {
            try
            {
                return await base.CloseAllJobsAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await CloseAllJobsAsync();
                }
                throw ex;
            }
        }

        public new LuaValue[] Diag(int type, string jobID)
        {
            try
            {
                return base.Diag(type, jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return Diag(type, jobID);
                }

                throw ex;
            }
        }

        public new async Task<DiagResponse> DiagAsync(int type, string jobID)
        {
            try
            {
                return await base.DiagAsync(type, jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await DiagAsync(type, jobID);
                }
                throw ex;
            }
        }

        public new LuaValue[] DiagEx(int type, string jobID)
        {
            try
            {
                return base.DiagEx(type, jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return DiagEx(type, jobID);
                }

                throw ex;
            }
        }

        public new async Task<LuaValue[]> DiagExAsync(int type, string jobID)
        {
            try
            {
                return await base.DiagExAsync(type, jobID);
            }
            catch (EndpointNotFoundException ex)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                {
                    SystemUtility.Singleton.OpenGridServerSafe();
                    return await DiagExAsync(type, jobID);
                }
                throw ex;
            }
        }
    }
}