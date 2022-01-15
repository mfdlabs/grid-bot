using System.Threading.Tasks;
using MFDLabs.Diagnostics;
using MFDLabs.Analytics.Google.Client;
using MFDLabs.Instrumentation;
using MFDLabs.Networking;

namespace MFDLabs.Analytics.Google
{
    public static class GoogleAnalyticsManager
    {
        private static bool _fitToUse = true;

        public static void Initialize(ICounterRegistry registry, string trackerId) =>
            _sharedGaClient = new GaClient(
                registry,
                new GaClientConfig(
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GoogleAnalyticsURL,
                    trackerId,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientMaxRedirects,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientRequestTimeout,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default
                        .GAClientCircuitBreakerFailuresAllowedBeforeTrip,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GAClientCircuitBreakerRetryInterval
                )
            );

        public static void TrackEvent(string clientId,
            string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1)
        {
            if (!_fitToUse) return;

            try
            {
                _sharedGaClient?.TrackEvent(clientId, $"{SystemGlobal.GetMachineId()} {category}", action, label,
                    value);
            }
            catch
            {
                _fitToUse = false;
            }
        }


        public static void TrackNetworkEvent(string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1) =>
            TrackEvent(NetworkingGlobal.LocalIp, category, action, label, value);

        public static Task TrackNetworkEventAsync(string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1) =>
            TrackEventAsync(NetworkingGlobal.LocalIp, category, action, label, value);

        public static async Task TrackEventAsync(string clientId,
            string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1)
        {
            if (!_fitToUse) return;

            if (_sharedGaClient == null) return;

            try
            {
                await _sharedGaClient.TrackEventAsync(clientId, $"{SystemGlobal.GetMachineId()} {category}", action,
                    label, value);
            }
            catch
            {
                _fitToUse = false;
            }
        }

        private static IGaClient _sharedGaClient;
    }
}