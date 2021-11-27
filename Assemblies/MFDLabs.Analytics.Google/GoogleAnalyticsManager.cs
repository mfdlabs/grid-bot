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

        public ICounterRegistry Registry { get; } = new CounterRegistry();

        public void Initialize(string trackerID)
        {
            _sharedGAClient = new GAClient(
                Registry,
                new GAClientConfig(
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GoogleAnalyticsURL,
                    trackerID,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientMaxRedirects,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientRequestTimeout,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientCircuitBreakerFailuresAllowedBeforeTrip,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientCircuitBreakerRetryInterval
                )
            );
        }

        public void TrackEvent(string clientID, string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            // Throw here?
            if (_sharedGAClient == null) return;

            _sharedGAClient.TrackEvent(clientID, $"{SystemGlobal.Singleton.GetMachineID()} {category}", action, label, value);
        }

        public void TrackNetworkEvent(string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            TrackEvent(NetworkingGlobal.Singleton.LocalIP, category, action, label, value);
        }

        public Task TrackNetworkEventAsync(string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            return TrackEventAsync(NetworkingGlobal.Singleton.LocalIP, category, action, label, value);
        }

        public async Task TrackEventAsync(string clientID, string category = "Server", string action = "ServerAction", string label = "None", int value = 1)
        {
            // Throw here?
            if (_sharedGAClient == null) return;

            await _sharedGAClient.TrackEventAsync(clientID, $"{SystemGlobal.Singleton.GetMachineID()} {category}", action, label, value);
        }

        private IGAClient _sharedGAClient;
    }
}
