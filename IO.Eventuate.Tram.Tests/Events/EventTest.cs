using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using IO.Eventuate.Tram.Events.Publisher;
using IO.Eventuate.Tram.Events.Common;
using NUnit.Framework.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace IO.Eventuate.Tram.Tests
{

    [TestClass]
    public class EventTest
    {
        long uniqueId = System.DateTime.Now.Ticks;
        String aggregateType;
        String aggregateId;

        IDomainEvent domainEvent;
        IDomainEventPublisher domainEventPublisher;
        TestEventConsumer consumer;
        public EventTest()
        {
            aggregateType = "Account" + uniqueId;
            aggregateId = "accountId" + uniqueId;
        }

        [TestInitialize]
        public void SetUp()
        {
            var host = new HostBuilder()
          .ConfigureServices((hostContext, services) =>
          {
              // Logging 
              services.AddLogging(builder =>
             {
                 builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                 builder.AddConsole();
                 builder.AddDebug();
             });
              // Kafka Transport
              services.AddEventuateTramSqlKafkaTransport(TestSettings.EventuateTramDbSchema, TestSettings.KafkaBootstrapServers, EventuateKafkaConsumerConfigurationProperties.Empty(),
                 (provider, o) =>
                 {
                     o.UseSqlServer(TestSettings.EventuateTramDbConnection);
                 });
              // Publisher
              services.AddEventuateTramEventsPublisher();
              // Consumer
              services.AddSingleton<TestEventConsumer>();
              // Dispatcher
              services.AddEventuateTramDomainEventDispatcher(Guid.NewGuid().ToString(), provider =>
             {
                 var consumer = provider.GetRequiredService<TestEventConsumer>();
                 return consumer.DomainEventHandlers(aggregateType);
             });
          }).Build();
            host.StartAsync().Wait();
            domainEventPublisher = host.Services.GetService<IDomainEventPublisher>();
            consumer = host.Services.GetService<TestEventConsumer>();

        }
        [TestMethod]
        public void shouldReceiveEvent()
        {
            domainEvent = new AccountDebited(uniqueId);
            domainEventPublisher.Publish(aggregateType, aggregateId, new List<IDomainEvent> { domainEvent });

            AccountDebited accountEvent;
            consumer.GetQueue().TryTake(out accountEvent, TimeSpan.FromSeconds(10));

            Assert.IsNotNull(accountEvent);
            Assert.AreEqual(uniqueId, accountEvent.getAmount());
        }

    }
}
