namespace LoyaltyProgramEventConsumer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using LoyaltyProgramEventConsumer.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public abstract class HttpEventSubcriber : IEventSubcriber<HttpResponseMessage>
    {
        private readonly string _eventHost;
        private readonly ILogger _logger;
        private int _chunkSize = 100;

        public HttpEventSubcriber(string eventHost, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(eventHost))
                throw new ArgumentException(nameof(eventHost));
            this._eventHost = eventHost;
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual bool IsValidEvent(HttpResponseMessage @event)
        {
            return @event.StatusCode == HttpStatusCode.OK;
        }

        public virtual async Task HandleEvents(HttpResponseMessage @event)
        {
            this._logger.LogTrace("Start Handle Events");
            var content = await @event.Content.ReadAsStringAsync();
            var events = JsonConvert.DeserializeObject<IEnumerable<Event>>(content);
            Console.WriteLine($"Number of eventst to handle: {events.Count()}");
            var lastSucceededEvent = 0L;
            foreach (var ev in events)
            {
                dynamic eventData = ev.Content;
                Console.WriteLine("product name from data: " + (string)eventData.offer.productName);
                if (ShouldSendNotification(eventData))
                {
                    var isSucceeded = await SendNotification(eventData).ConfigureAwait(false);
                    if (!isSucceeded)
                        return;
                }
                lastSucceededEvent = Math.Max(lastSucceededEvent, ev.SequenceNumber + 1);
            }
            await WriteStartNumber(lastSucceededEvent);
            this._logger.LogTrace("Start Handle Events");
        }

        public async Task<HttpResponseMessage> ReadEvents()
        {
            var startNumber = await ReadStartNumber().ConfigureAwait(false);
            this._logger.LogTrace($"Start Read Events from host:{this._eventHost}, with top:{startNumber} & end:{startNumber + this._chunkSize}");
            var eventResource = $"/events/?start={startNumber}&end={startNumber + this._chunkSize}";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.BaseAddress = new Uri(this._eventHost);
                var response = await httpClient.GetAsync(eventResource).ConfigureAwait(false);
                PrettyPrintResponse(response);
                this._logger.LogTrace($"End Read Events from host:{this._eventHost}");
                return response;
            }
        }

        public async Task ReadAndHandleEvents()
        {
            var @event = await ReadEvents();
            if (IsValidEvent(@event))
                await HandleEvents(@event);
        }

        private bool ShouldSendNotification(dynamic eventData)
        {
            // decide if notification should be sent base on business rules
            return true;
        }

        private async Task<bool> SendNotification(dynamic eventData)
        {
            // user HttpClient to send command to notification microservice
            // return true if the command succeeded, false otherwise
            return true;
        }

        private async Task<long> ReadStartNumber()
        {
            // Read start number from database
            return 0;
        }

        private async Task WriteStartNumber(long startNumber)
        {
            // Write start number to database
        }

        private async void PrettyPrintResponse(HttpResponseMessage response)
        {
            Console.WriteLine("Status code: " + response?.StatusCode.ToString() ?? "command failed");
            Console.WriteLine("Headers: " + response?.Headers.Aggregate("", (acc, h) => acc + "\n\t" + h.Key + ": " + h.Value) ?? "");
            Console.WriteLine("Body: " + await response?.Content.ReadAsStringAsync() ?? "");
        }
    }
}