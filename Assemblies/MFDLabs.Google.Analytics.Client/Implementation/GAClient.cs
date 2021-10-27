using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Google.Analytics.Client
{
    public class GAClient : IGAClient
    {
        public GAClient(ICounterRegistry counterRegistry, GAClientConfig config)
        {
            var settings = new GAClientSettings(config);
            var httpClientBuilder = new GAHttpClientBuilder(counterRegistry, settings, config);
            var httpClient = httpClientBuilder.Build();
            _config = config;
            _sender = new FormDataRequestSender(httpClient, new HttpRequestBuilderSettings(settings.Endpoint));
        }

        private IEnumerable<(string, string)> BuildSharedBody(string clientID, string hitType, string source, bool shouldClose)
        {
            yield return ("v", "1");
            yield return ("tid", _config.TrackerID);
            yield return ("cid", clientID);
            yield return ("t", hitType);
            yield return ("ds", source);
            yield return ("sc", shouldClose ? "end" : "start");
            yield break;
        }

        private IEnumerable<(string, string)> BuildBodyForEventRequest(string category, string eventName, string label, int value)
        {
            yield return ("ec", category);
            yield return ("ea", eventName);
            yield return ("ev", value.ToString());
            yield return ("el", label);
            yield break;
        }

        private IEnumerable<(string, string)> BuildBodyForItemRequest(string transactionID, string itemName, double itemPrice, int itemQuantity, string itemCategory)
        {
            yield return ("ti", transactionID);
            yield return ("in", itemName);
            yield return ("ip", itemPrice.ToString());
            yield return ("iq", itemQuantity.ToString());
            yield return ("ic", itemCategory.ToString());
            yield break;
        }

        private IEnumerable<(string, string)> BuildBodyForPageViewRequest(string documentUrl)
        {
            yield return ("dl", documentUrl);
            yield break;
        }

        private IEnumerable<(string, string)> BuildBodyForTransactionRequest(string transactionID, string transactionAffiliation, double transactionRevenue, double transactionTax)
        {
            yield return ("ti", transactionID);
            yield return ("ta", transactionAffiliation);
            yield return ("tr", transactionRevenue.ToString());
            yield return ("tt", transactionTax.ToString());
            yield break;
        }

        public void TrackEvent(string clientID, string category, string eventName, string label = "None", int value = 0, string source = "Server", bool shouldClose = false)
        {
            if (category.IsNullOrWhiteSpace()) throw new ArgumentNullException("category");
            if (eventName.IsNullOrWhiteSpace()) throw new ArgumentNullException("eventName");
            if (label.IsNullOrWhiteSpace()) throw new ArgumentNullException("label");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");
            if (value < 0) throw new ArgumentOutOfRangeException("value", "The value cannot be negative");

            var request = BuildSharedBody(clientID, "event", source, shouldClose).Concat(
                BuildBodyForEventRequest(
                    category,
                    eventName,
                    label,
                    value
                )
            );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackEventAsync(string clientID, string category, string eventName, string label = "None", int value = 0, string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (category.IsNullOrWhiteSpace()) throw new ArgumentNullException("category");
            if (eventName.IsNullOrWhiteSpace()) throw new ArgumentNullException("eventName");
            if (label.IsNullOrWhiteSpace()) throw new ArgumentNullException("label");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");
            if (value < 0) throw new ArgumentOutOfRangeException("value", "The value cannot be negative");
            if (cancellationToken == default(CancellationToken)) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientID, "event", source, shouldClose)
                .Concat(
                    BuildBodyForEventRequest(
                        category,
                        eventName,
                        label,
                        value
                    )
                );

            return _sender.SendRequestAsync(HttpMethod.Post, "/collect", cancellationToken, request);
        }
        public void TrackItem(string clientID, string transactionID, string itemName, double itemPrice = 0, int itemQuantity = 1, string itemCategory = "None", string source = "Server", bool shouldClose = false)
        {
            if (transactionID.IsNullOrWhiteSpace()) throw new ArgumentNullException("transactionID");
            if (itemName.IsNullOrWhiteSpace()) throw new ArgumentNullException("itemName");
            if (itemPrice < 0) throw new ArgumentOutOfRangeException("itemPrice", "The value cannot be negative");
            if (itemQuantity < 1) throw new ArgumentOutOfRangeException("itemPrice", "The value cannot be negative or 0");
            if (itemCategory.IsNullOrWhiteSpace()) throw new ArgumentNullException("itemCategory");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");

            var request = BuildSharedBody(clientID, "event", source, shouldClose)
                .Concat(
                    BuildBodyForItemRequest(
                        transactionID,
                        itemName,
                        itemPrice,
                        itemQuantity,
                        itemCategory
                    )
                );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackItemAsync(string clientID, string transactionID, string itemName, double itemPrice = 0, int itemQuantity = 1, string itemCategory = "None", string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (transactionID.IsNullOrWhiteSpace()) throw new ArgumentNullException("transactionID");
            if (itemName.IsNullOrWhiteSpace()) throw new ArgumentNullException("itemName");
            if (itemPrice < 0) throw new ArgumentOutOfRangeException("itemPrice", "The value cannot be negative");
            if (itemQuantity < 1) throw new ArgumentOutOfRangeException("itemPrice", "The value cannot be negative or 0");
            if (itemCategory.IsNullOrWhiteSpace()) throw new ArgumentNullException("itemCategory");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");
            if (cancellationToken == default(CancellationToken)) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientID, "event", source, shouldClose)
                .Concat(
                    BuildBodyForItemRequest(
                        transactionID,
                        itemName,
                        itemPrice,
                        itemQuantity,
                        itemCategory
                    )
                );

            return _sender.SendRequestAsync(HttpMethod.Post, "/collect", cancellationToken, request);
        }
        public void TrackPageView(string clientID, string documentLocationUrl, string source = "Server", bool shouldClose = false)
        {
            if (documentLocationUrl.IsNullOrWhiteSpace()) throw new ArgumentNullException("documentLocationUrl");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");

            var request = BuildSharedBody(clientID, "event", source, shouldClose)
                .Concat(
                    BuildBodyForPageViewRequest(
                        documentLocationUrl
                    )
                );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackPageViewAsync(string clientID, string documentLocationUrl, string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (documentLocationUrl.IsNullOrWhiteSpace()) throw new ArgumentNullException("documentLocationUrl");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");
            if (cancellationToken == default(CancellationToken)) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientID, "event", source, shouldClose)
                .Concat(
                    BuildBodyForPageViewRequest(
                        documentLocationUrl
                    )
                );

            return _sender.SendRequestAsync(HttpMethod.Post, "/collect", cancellationToken, request);
        }
        public void TrackTransaction(string clientID, string transactionID, string transactionAffiliation, double transactionRevenue = 0, double transactionTax = 0, string source = "Server", bool shouldClose = false)
        {
            if (transactionID.IsNullOrWhiteSpace()) throw new ArgumentNullException("transactionID");
            if (transactionAffiliation.IsNullOrWhiteSpace()) throw new ArgumentNullException("transactionAffiliation");
            if (transactionRevenue < 0) throw new ArgumentOutOfRangeException("transactionRevenue", "The value cannot be negative");
            if (transactionTax < 0) throw new ArgumentOutOfRangeException("transactionTax", "The value cannot be negative");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");

            var request = BuildSharedBody(clientID, "event", source, shouldClose)
                .Concat(
                    BuildBodyForTransactionRequest(
                        transactionID,
                        transactionAffiliation,
                        transactionRevenue,
                        transactionTax
                    )
                );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackTransactionAsync(string clientID, string transactionID, string transactionAffiliation, double transactionRevenue = 0, double transactionTax = 0, string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (transactionID.IsNullOrWhiteSpace()) throw new ArgumentNullException("transactionID");
            if (transactionAffiliation.IsNullOrWhiteSpace()) throw new ArgumentNullException("transactionAffiliation");
            if (transactionRevenue < 0) throw new ArgumentOutOfRangeException("transactionRevenue", "The value cannot be negative");
            if (transactionTax < 0) throw new ArgumentOutOfRangeException("transactionTax", "The value cannot be negative");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException("source");
            if (cancellationToken == default(CancellationToken)) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientID, "event", source, shouldClose)
                .Concat(
                    BuildBodyForTransactionRequest(
                        transactionID,
                        transactionAffiliation,
                        transactionRevenue,
                        transactionTax
                    )
                );

            return _sender.SendRequestAsync(HttpMethod.Post, "/collect", cancellationToken, request);
        }

        private readonly FormDataRequestSender _sender;
        private readonly GAClientConfig _config;
    }
}
