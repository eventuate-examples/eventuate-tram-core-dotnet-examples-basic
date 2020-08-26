using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using IO.Eventuate.Tram.Events.Publisher;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Tests.TestHelpers;
using NUnit.Framework.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
        TestServicesHost testServicesHost;
        public EventTest()
        {
            aggregateType = "Account" + uniqueId;
            aggregateId = "accountId" + uniqueId;
        }

        [TestInitialize]
        public void SetUp()
        {
            testServicesHost = new TestServicesHost();
            testServicesHost.SetAggregateType(aggregateType);
            IHost host = testServicesHost.SetUpHost();
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
