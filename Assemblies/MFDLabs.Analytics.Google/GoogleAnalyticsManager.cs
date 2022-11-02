using System.Threading.Tasks;
using MFDLabs.Networking;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;
using MFDLabs.Analytics.Google.MetricsProtocol.Client;
using MFDLabs.Analytics.Google.UniversalAnalytics.Client;

namespace MFDLabs.Analytics.Google
{
    public static class GoogleAnalyticsManager
    {
        public static void Initialize(ICounterRegistry registry, string trackerId, string metricsId, string apiSecret, bool enableServersideValidation = false)
        {
            _sharedGaUniversalClient = new GaUniversalClient(
                registry,
                new GaUniversalClientConfig(
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GoogleAnalyticsURL,
                    trackerId,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GaUniversalClientMaxRedirects,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GaUniversalClientRequestTimeout,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GaUniversalClientCircuitBreakerFailuresAllowedBeforeTrip,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GaUnversalClientCircuitBreakerRetryInterval
                )
            );

            _sharedGa4Client = new Ga4Client(
                registry,
                new Ga4ClientConfig(
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.GoogleAnalyticsURL,
                    metricsId,
                    apiSecret,
                    enableServersideValidation,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.Ga4ClientMaxRedirects,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.Ga4ClientRequestTimeout,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.Ga4ClientCircuitBreakerFailuresAllowedBeforeTrip,
                    global::MFDLabs.Analytics.Google.Properties.Settings.Default.Ga4ClientCircuitBreakerRetryInterval
                )
            );


        }


        public static void TrackEvent(string clientId,
            string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1)
        {
            try
            {
                _sharedGaUniversalClient?.TrackEvent(clientId, $"{SystemGlobal.GetMachineId()} {category}", action, label, value, SystemGlobal.GetMachineId());
                _sharedGa4Client?.FireEvent(clientId, "track_event", new
                {
                    category = $"{SystemGlobal.GetMachineId()} {category}",
                    action,
                    label,
                    value = value.ToString(),
                    source = SystemGlobal.GetMachineId()
                }, null);
            }
            catch
            {
            }
        }

        public static void TrackPageView(string clientId, string documentLocationUrl)
        {
            try
            {
                _sharedGaUniversalClient?.TrackPageView(clientId, $"{SystemGlobal.GetMachineId()} @ {documentLocationUrl}", SystemGlobal.GetMachineId());
                _sharedGa4Client?.FireEvent(clientId, "page_view", new
                {
                    page_location = $"{SystemGlobal.GetMachineId()} @ {documentLocationUrl}",
                    page_referrer = SystemGlobal.GetMachineId()
                }, null);
            }
            catch
            {
            }
        }

        public static async Task TrackPageViewAsync(string clientId, string documentLocationUrl)
        {
            try
            {
                if (_sharedGaUniversalClient != null)
                    await _sharedGaUniversalClient.TrackPageViewAsync(clientId, $"$({SystemGlobal.GetMachineId()}).{documentLocationUrl}", SystemGlobal.GetMachineId()).ConfigureAwait(false);

                if (_sharedGa4Client != null)
                    await _sharedGa4Client.FireEventAsync(clientId, "page_view", null, new
                    {
                        page_location = $"{SystemGlobal.GetMachineId()} @ {documentLocationUrl}",
                        page_referrer = SystemGlobal.GetMachineId()
                    }, null).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        public static void TrackNetworkEvent(string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1) =>
            TrackEvent(NetworkingGlobal.LocalIp, category, action, label, value);

        public static async Task TrackNetworkEventAsync(string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1) =>
            await TrackEventAsync(NetworkingGlobal.LocalIp, category, action, label, value);

        public static async Task TrackEventAsync(string clientId,
            string category = "Server",
            string action = "ServerAction",
            string label = "None",
            int value = 1)
        {
            try
            {
                if (_sharedGaUniversalClient != null)
                    await _sharedGaUniversalClient.TrackEventAsync(clientId, $"{SystemGlobal.GetMachineId()} {category}", action, label, value, SystemGlobal.GetMachineId()).ConfigureAwait(false);

                if (_sharedGa4Client != null)
                    await _sharedGa4Client.FireEventAsync(clientId, "track_event", null, new
                    {
                        category = $"{SystemGlobal.GetMachineId()} {category}",
                        action,
                        label,
                        value = value.ToString(),
                        source = SystemGlobal.GetMachineId()
                    }, null).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private static IGaUniversalClient _sharedGaUniversalClient;
        private static IGa4Client _sharedGa4Client;
    }
}