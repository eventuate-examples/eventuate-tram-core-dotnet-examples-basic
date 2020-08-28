using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace IO.Eventuate.Tram.Tests
{
    public class TestEventConsumer
    {
        private readonly ILogger<TestEventConsumer> _logger;
        private BlockingCollection<AccountDebited> queue = new BlockingCollection<AccountDebited>();
        public TestEventConsumer(ILogger<TestEventConsumer> logger)
        {
            _logger = logger;
        }
        public DomainEventHandlers DomainEventHandlers(string aggregateType)
        {
            return DomainEventHandlersBuilder.ForAggregateType(aggregateType)
                .OnEvent<AccountDebited>(HandleAccountDebited)
                .Build();
        }

        private void HandleAccountDebited(IDomainEventEnvelope<AccountDebited> debitEvent)
        {
            queue.Add(debitEvent.Event);
        }
        public BlockingCollection<AccountDebited> GetQueue()
        {
            return queue;
        }
    }
}
