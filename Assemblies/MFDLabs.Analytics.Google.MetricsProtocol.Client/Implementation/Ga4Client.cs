namespace MFDLabs.Analytics.Google.MetricsProtocol.Client;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Http;
using Models;
using Http.Client;
using Instrumentation;
using Text.Extensions;

#nullable enable

public class Ga4Client : IGa4Client
{
    public Ga4Client(ICounterRegistry counterRegistry, Ga4ClientConfig config)
    {
        var settings = new Ga4ClientSettings(config);
        var httpClientBuilder = new Ga4HttpClientBuilder(counterRegistry, settings, config);
        var httpClient = httpClientBuilder.Build();

        var validatorHttpClientBuilder = new Ga4ValidatorHttpClientBuilder(counterRegistry, settings, config);
        var validatorHttpClient = validatorHttpClientBuilder.Build();

        _config = config;
        _sender = new HttpRequestSender(httpClient, new HttpRequestBuilder(settings.Endpoint));

        _validatorSender = new HttpRequestSender(validatorHttpClient, new HttpRequestBuilder(settings.Endpoint));
    }

    private IEnumerable<(string, string)> BuildSharedQuery()
    {
        if (_config.MetricsId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(_config.MetricsId));
        if (_config.ApiSecret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(_config.ApiSecret));

        yield return ("measurement_id", _config.MetricsId);
        yield return ("api_secret", _config.ApiSecret);
    }

    private static object ToSnakeCase(object obj)
    {
        IDictionary<string, object> newData = new Dictionary<string, object>();

        foreach (var prop in obj.GetType().GetProperties())
            newData[prop.Name.ToSnakeCase()] = prop.GetValue(obj);

        return newData;
    }

    private static object ToFullSnakeCase(object obj)
    {
        IDictionary<string, object> newData = new Dictionary<string, object>();

        foreach (var prop in obj.GetType().GetProperties())
            newData[prop.Name.ToSnakeCase()] = ((string)prop.GetValue(obj)).ToSnakeCase();

        return newData;
    }

    private void ValidateEventServerSide(object rawEvent)
    {
        if (!_config.ServerSideValidationEnabled) return;

        const string url = "/debug/mp/collect";

        var response = _validatorSender.SendRequestWithJsonBody<object, Ga4ValidationResponse>(
            HttpMethod.Post,
            url,
            rawEvent,
            BuildSharedQuery()
        );

        if (response is not null && response.ValidationMessages is not null)
        {
            if (!response.ValidationMessages.Any()) return;

            var message = "";
            foreach (var validationMessage in response.ValidationMessages)
                message +=
                    $"{validationMessage.ValidationCode} " +
                    $"({validationMessage.FieldPath}): " +
                    $"{validationMessage.Description}\n";

            throw new ApplicationException(message);
        }
    }

    private async Task ValidateEventServerSideAsync(object rawEvent, CancellationToken cancellationToken)
    {
        if (!_config.ServerSideValidationEnabled) return;

        const string url = "/debug/mp/collect";

        var response = await _validatorSender.SendRequestWithJsonBodyAsync<object, Ga4ValidationResponse>(
            HttpMethod.Post,
            url,
            rawEvent,
            cancellationToken,
            BuildSharedQuery()
        );

        if (response is not null && response.ValidationMessages is not null)
        {
            if (!response.ValidationMessages.Any()) return;

            var message = "";
            foreach (var validationMessage in response.ValidationMessages)
                message +=
                    $"{validationMessage.ValidationCode} " +
                    $"({validationMessage.FieldPath}): " +
                    $"{validationMessage.Description}\n";

            throw new ApplicationException(message);
        }
    }

    private static void ValidateEvent(Ga4EventRequest @event)
    {
        var eventNames = @event.Events.Where(e => e is Ga4Event evtModel && _reservedEventNames.Contains(evtModel.Name));

        if (eventNames.Any())
            throw new ApplicationException(
                string.Format(
                    "Event validation failed. Event name(s) '{0}' are reserved.",
                    eventNames.Select(e => e is Ga4Event evtModel ? evtModel.Name : "<unknown>").Join(", ")
                )
            );

        if (@event.UserProperties is not null)
        {
            var propertyNames = @event.UserProperties
                .GetType()
                .GetProperties();

            var reservedPropertyNames = propertyNames
                .Where(p => _reservedUserPropertyNames.Contains(p.Name));

            if (reservedPropertyNames.Any())
                throw new ApplicationException(
                    string.Format(
                        "Event validation failed. User property name(s) '{0}' are reserved.",
                        reservedPropertyNames.Select(e => e.Name).Join(", ")
                    )
                );

            var disallowedPropertyNames = _disallowedUserPropertyStartStrings.Any(
                s => propertyNames.Any(p => p.Name.StartsWith(s))
            );

            if (disallowedPropertyNames)
                throw new ApplicationException(
                    string.Format(
                        "Event validation failed. User property name(s) '{0}' are disallowed.",
                        reservedPropertyNames.Select(e => e.Name).Join(", ")
                    )
                );
        }

        foreach (var evt in @event.Events)
        {
            if (evt is not Ga4Event evtModel) continue;
            if (evtModel.Params is null) continue;

            var propertyNames = evtModel.Params
                .GetType()
                .GetProperties();

            var reservedParamNames = propertyNames
                .Where(p => _reservedParameterNames.Contains(p.Name));

            if (reservedParamNames.Any())
                throw new ApplicationException(
                    string.Format(
                        "Event validation failed. Event parameter name(s) '{0}' are reserved.",
                        reservedParamNames.Select(e => e.Name).Join(", ")
                    )
                );

            var disallowedParamNames = _disallowedParameterStartStrings.Any(
                s => propertyNames.Any(p => p.Name.StartsWith(s))
            );

            if (disallowedParamNames)
                throw new ApplicationException(
                    string.Format(
                        "Event validation failed. Event parameter name(s) '{0}' are disallowed.",
                        reservedParamNames.Select(e => e.Name).Join(", ")
                    )
                );
        }
    }

    private void SendInternal(Ga4EventRequest request)
    {
        const string url = "/mp/collect";

        var events = request.Events.ToList();
        for (int i = 0; i < request.Events.Count; i++)
        {
            object @event = ToSnakeCase(request.Events.ElementAtOrDefault(i));
            events.RemoveAt(i);
            events.Insert(i, @event);
        }

        request.Events = events;

        var snakeCase = ToSnakeCase(request);

        ValidateEvent(request);
        ValidateEventServerSide(snakeCase);

        _sender.SendRequestWithJsonBody(HttpMethod.Post, url, snakeCase, BuildSharedQuery());
    }

    private async Task SendInternalAsync(Ga4EventRequest request, CancellationToken cancellationToken)
    {
        const string url = "/mp/collect";

        for (int i = 0; i < request.Events.Count; i++)
        {
            dynamic @event = ToSnakeCase(request.Events.ElementAtOrDefault(i));
            request.Events.ToList().Insert(i, @event);
        }

        var snakeCase = ToSnakeCase(request);

        ValidateEvent(request);
        await ValidateEventServerSideAsync(snakeCase, cancellationToken);

        await _sender.SendRequestWithJsonBodyAsync(HttpMethod.Post, url, snakeCase, cancellationToken, BuildSharedQuery());
    }

    public void FireEvent(string clientId, string eventName, object? @params, object? properties)
    {
        if (clientId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(clientId));
        if (eventName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(eventName));
        properties ??= new { };
        @params ??= new { };

        properties = ToSnakeCase(properties);
        @params = ToSnakeCase(@params);

        var request = new Ga4EventRequest
        {
            ClientId = clientId,
            UserProperties = properties
        };

        request.Events.Add(
            new Ga4Event()
            {
                Name = eventName,
                Params = @params
            }
        );

        SendInternal(request);
    }

    public async Task FireEventAsync(string clientId, string eventName, CancellationToken? cancellationToken,
        object? @params, object? properties)
    {
        if (clientId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(clientId));
        if (eventName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(eventName));
        properties ??= new { };
        @params ??= new { };

        properties = ToSnakeCase(properties);
        @params = ToSnakeCase(@params);

        var request = new Ga4EventRequest
        {
            ClientId = clientId,
            UserProperties = properties
        };

        request.Events.Add(
            new Ga4Event()
            {
                Name = eventName,
                Params = @params
            }
        );

        await SendInternalAsync(request, cancellationToken ?? CancellationToken.None);
    }

    private readonly HttpRequestSender _sender;
    private readonly HttpRequestSender _validatorSender;
    private readonly Ga4ClientConfig _config;

    private static readonly string[] _reservedEventNames = new[]
    {
        "ad_activeview",
        "ad_click",
        "ad_exposure",
        "ad_impression",
        "ad_query",
        "adunit_exposure",
        "app_clear_data",
        "app_install",
        "app_update",
        "app_remove",
        "error",
        "first_open",
        "first_visit",
        "in_app_purchase",
        "notification_dismiss",
        "notification_foreground",
        "notification_open",
        "notification_receive",
        "os_update",
        "screen_view",
        "session_start",
        "user_engagement",
    };

    private static readonly string[] _reservedParameterNames = new[] { "firebase_conversion" };

    private static readonly string[] _disallowedParameterStartStrings = new[]
    {
        "google_",
        "ga_",
        "firebase_"
    };

    private static readonly string[] _reservedUserPropertyNames = new[]
    {
        "first_open_time",
        "first_visit_time",
        "last_deep_link_referrer",
        "user_id",
        "first_open_after_install",
    };

    private static readonly string[] _disallowedUserPropertyStartStrings = new[]
    {
        "google_",
        "ga_",
        "firebase_"
    };
}
