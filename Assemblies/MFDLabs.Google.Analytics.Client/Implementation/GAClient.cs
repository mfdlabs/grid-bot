using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Google.Analytics.Client
{
    public class GaClient : IGaClient
    {
        public GaClient(ICounterRegistry counterRegistry, GaClientConfig config)
        {
            var settings = new GaClientSettings(config);
            var httpClientBuilder = new GaHttpClientBuilder(counterRegistry, settings, config);
            var httpClient = httpClientBuilder.Build();
            _config = config;
            _sender = new FormDataRequestSender(httpClient, new HttpRequestBuilderSettings(settings.Endpoint));
        }

        private IEnumerable<(string, string)> BuildSharedBody(string clientId,
            string hitType,
            string source,
            bool shouldClose)
        {
            yield return ("v", "1");
            yield return ("tid", _config.TrackerId);
            yield return ("cid", clientId);
            yield return ("t", hitType);
            yield return ("ds", source);
            yield return ("sc", shouldClose ? "end" : "start");
        }

        private static IEnumerable<(string, string)> BuildBodyForEventRequest(string category,
            string eventName,
            string label,
            int value)
        {
            yield return ("ec", category);
            yield return ("ea", eventName);
            yield return ("ev", value.ToString());
            yield return ("el", label);
        }

        private static IEnumerable<(string, string)> BuildBodyForItemRequest(string transactionId,
            string itemName,
            double itemPrice,
            int itemQuantity,
            string itemCategory)
        {
            yield return ("ti", transactionId);
            yield return ("in", itemName);
            yield return ("ip", itemPrice.ToString(CultureInfo.InvariantCulture));
            yield return ("iq", itemQuantity.ToString());
            yield return ("ic", itemCategory);
        }

        private static IEnumerable<(string, string)> BuildBodyForPageViewRequest(string documentUrl)
        {
            yield return ("dl", documentUrl);
        }

        private static IEnumerable<(string, string)> BuildBodyForTransactionRequest(string transactionId,
            string transactionAffiliation,
            double transactionRevenue,
            double transactionTax)
        {
            yield return ("ti", transactionId);
            yield return ("ta", transactionAffiliation);
            yield return ("tr", transactionRevenue.ToString(CultureInfo.InvariantCulture));
            yield return ("tt", transactionTax.ToString(CultureInfo.InvariantCulture));
        }

        public void TrackEvent(string clientId,
            string category,
            string eventName,
            string label = "None",
            int value = 0,
            string source = "Server",
            bool shouldClose = false)
        {
            if (category.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(category));
            if (eventName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(eventName));
            if (label.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(label));
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "The value cannot be negative");

            var request = BuildSharedBody(clientId, "event", source, shouldClose).Concat(
                BuildBodyForEventRequest(
                    category,
                    eventName,
                    label,
                    value
                )
            );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackEventAsync(string clientId,
            string category,
            string eventName,
            string label = "None",
            int value = 0,
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default)
        {
            if (category.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(category));
            if (eventName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(eventName));
            if (label.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(label));
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "The value cannot be negative");
            if (cancellationToken == default) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientId, "event", source, shouldClose)
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
        public void TrackItem(string clientId,
            string transactionId,
            string itemName,
            double itemPrice = 0,
            int itemQuantity = 1,
            string itemCategory = "None",
            string source = "Server",
            bool shouldClose = false)
        {
            if (transactionId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(transactionId));
            if (itemName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(itemName));
            if (itemPrice < 0) throw new ArgumentOutOfRangeException(nameof(itemPrice), "The value cannot be negative");
            if (itemQuantity < 1) throw new ArgumentOutOfRangeException(nameof(itemPrice), "The value cannot be negative or 0");
            if (itemCategory.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(itemCategory));
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));

            var request = BuildSharedBody(clientId, "event", source, shouldClose)
                .Concat(
                    BuildBodyForItemRequest(
                        transactionId,
                        itemName,
                        itemPrice,
                        itemQuantity,
                        itemCategory
                    )
                );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackItemAsync(string clientId,
            string transactionId,
            string itemName,
            double itemPrice = 0,
            int itemQuantity = 1,
            string itemCategory = "None",
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default)
        {
            if (transactionId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(transactionId));
            if (itemName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(itemName));
            if (itemPrice < 0) throw new ArgumentOutOfRangeException(nameof(itemPrice), "The value cannot be negative");
            if (itemQuantity < 1) throw new ArgumentOutOfRangeException(nameof(itemPrice), "The value cannot be negative or 0");
            if (itemCategory.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(itemCategory));
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));
            if (cancellationToken == default) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientId, "event", source, shouldClose)
                .Concat(
                    BuildBodyForItemRequest(
                        transactionId,
                        itemName,
                        itemPrice,
                        itemQuantity,
                        itemCategory
                    )
                );

            return _sender.SendRequestAsync(HttpMethod.Post, "/collect", cancellationToken, request);
        }
        public void TrackPageView(string clientId,
            string documentLocationUrl,
            string source = "Server",
            bool shouldClose = false)
        {
            if (documentLocationUrl.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(documentLocationUrl));
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));

            var request = BuildSharedBody(clientId, "event", source, shouldClose)
                .Concat(
                    BuildBodyForPageViewRequest(
                        documentLocationUrl
                    )
                );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackPageViewAsync(string clientId,
            string documentLocationUrl,
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default)
        {
            if (documentLocationUrl.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(documentLocationUrl));
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));
            if (cancellationToken == default) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientId, "event", source, shouldClose)
                .Concat(
                    BuildBodyForPageViewRequest(
                        documentLocationUrl
                    )
                );

            return _sender.SendRequestAsync(HttpMethod.Post, "/collect", cancellationToken, request);
        }
        public void TrackTransaction(string clientId,
            string transactionId,
            string transactionAffiliation,
            double transactionRevenue = 0,
            double transactionTax = 0,
            string source = "Server",
            bool shouldClose = false)
        {
            if (transactionId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(transactionId));
            if (transactionAffiliation.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(transactionAffiliation));
            if (transactionRevenue < 0) throw new ArgumentOutOfRangeException(nameof(transactionRevenue), "The value cannot be negative");
            if (transactionTax < 0) throw new ArgumentOutOfRangeException(nameof(transactionTax), "The value cannot be negative");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));

            var request = BuildSharedBody(clientId, "event", source, shouldClose)
                .Concat(
                    BuildBodyForTransactionRequest(
                        transactionId,
                        transactionAffiliation,
                        transactionRevenue,
                        transactionTax
                    )
                );

            _sender.SendRequest(HttpMethod.Post, "/collect", request);
        }
        public Task TrackTransactionAsync(string clientId,
            string transactionId,
            string transactionAffiliation,
            double transactionRevenue = 0,
            double transactionTax = 0,
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default)
        {
            if (transactionId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(transactionId));
            if (transactionAffiliation.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(transactionAffiliation));
            if (transactionRevenue < 0) throw new ArgumentOutOfRangeException(nameof(transactionRevenue), "The value cannot be negative");
            if (transactionTax < 0) throw new ArgumentOutOfRangeException(nameof(transactionTax), "The value cannot be negative");
            if (source.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(source));
            if (cancellationToken == default) cancellationToken = CancellationToken.None;

            var request = BuildSharedBody(clientId, "event", source, shouldClose)
                .Concat(
                    BuildBodyForTransactionRequest(
                        transactionId,
                        transactionAffiliation,
                        transactionRevenue,
                        transactionTax
                    )
                );

            return _sender.SendRequestAsync(HttpMethod.Post, "/collect", cancellationToken, request);
        }

        private readonly FormDataRequestSender _sender;
        private readonly GaClientConfig _config;
    }
}
