﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Glimpse.Agent
{
    public class HttpMessagePublisher : IMessagePublisher, IDisposable
    {
        private readonly ISubject<IMessage> _listenerSubject;
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpHandler;

        public HttpMessagePublisher()
        {
            _listenerSubject = new Subject<IMessage>();

            _httpHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpHandler);

            // ensure off-request message transport is obsered onto a different thread 
            _listenerSubject.Buffer(TimeSpan.FromMilliseconds(100)).Subscribe(x =>
            {
                // TODO: would be nice if the buffer only triggered when it had values
                if (x.Any())
                {
                    Observable.Start(async () => await Process(x), TaskPoolScheduler.Default);
                }
            });
        }

        public void PublishMessage(IMessage message)
        {
            _listenerSubject.OnNext(message);
        }

        public async Task Process(IEnumerable<IMessage> messages)
        {
            // TODO: Needs error handelling
            try
            {
                // TODO: Shouldn't use this :( bad on perf, do manually
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5210/Glimpse/AgentMessage", messages);

                // Check that response was successful or throw exception
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                // TODO: Bad thing happened
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _httpHandler.Dispose();
        }
    }
}