using System.Threading.Tasks;
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.Google.Analytics.Client;
using MFDLabs.Instrumentation;
using MFDLabs.Networking;

namespace MFDLabs.Analytics.Google
{
    public sealed class GoogleAnalyticsManager : SingletonBase<GoogleAnalyticsManager>
    {
        private ICounterRegistry Registry { get; } = new CounterRegistry();

        public void Initialize(string trackerId)
        {
            _sharedGaClient = new GaClient(
                Registry,
                new GaClientConfig(
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GoogleAnalyticsURL,
                    trackerId,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientMaxRedirects,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientRequestTimeout,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientCircuitBreakerFailuresAllowedBeforeTrip,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientCircuitBreakerRetryInterval
                )
            );
        }

        public void TrackEvent(string clientId, string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            // Throw here?
            _sharedGaClient?.TrackEvent(clientId, $"{SystemGlobal.GetMachineId()} {category}", action, label, value);
        }

        public void TrackNetworkEvent(string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            TrackEvent(NetworkingGlobal.LocalIp, category, action, label, value);
        }

        public Task TrackNetworkEventAsync(string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            return TrackEventAsync(NetworkingGlobal.LocalIp, category, action, label, value);
        }

        public async Task TrackEventAsync(string clientId, string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            if (_sharedGaClient == null) return;

            await _sharedGaClient.TrackEventAsync(clientId, $"{SystemGlobal.GetMachineId()} {category}", action, label, value);
        }

        private IGaClient _sharedGaClient;
    }
}
