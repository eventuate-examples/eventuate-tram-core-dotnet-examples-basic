﻿using System;
using System.Collections.Generic;
using IO.Eventuate.Tram.Messaging.Producer;
using IO.Eventuate.Tram.Consumer.Kafka;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using IO.Eventuate.Tram.Messaging.Consumer;
using System.Collections.Concurrent;

namespace IO.Eventuate.Tram.Tests
{

    [TestClass]
    public class MessageTest
    {
        public string uniqueId = System.DateTime.Now.Ticks.ToString();
        public string subscriberId = "";
        public string destination = "";
        public string payload = "";
        IMessageProducer messageProducer;
        IMessageConsumer messageConsumer;

        private BlockingCollection<IMessage> queue = new BlockingCollection<IMessage>();
        public ISet<string> channels = new HashSet<string>();

        public MessageTest()
        {
            uniqueId = System.DateTime.Now.Ticks.ToString();
            subscriberId = "subscriberId" + uniqueId;
            destination = "destination" + uniqueId;
            payload = "Hello" + uniqueId;
            channels.Add(destination);
        }

        [TestInitialize]
        public void SetUp()
        {
            var serviceCollection = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddConsole();
                builder.AddDebug();
            });

            serviceCollection.AddEventuateTramSqlKafkaTransport(TestSettings.EventuateTramDbSchema,
              TestSettings.KafkaBootstrapServers,
            	EventuateKafkaConsumerConfigurationProperties.Empty(), (provider, dbContextOptionsBuilder) =>
            	{
            		dbContextOptionsBuilder.UseSqlServer(TestSettings.EventuateTramDbConnection);
            	});

            var services = serviceCollection.BuildServiceProvider();

            messageConsumer = services.GetRequiredService<IMessageConsumer>();
            messageProducer = services.GetRequiredService<IMessageProducer>();

        }
        [TestMethod]
        public void ShouldReceiveMessage()
        {
            messageConsumer.Subscribe(subscriberId, channels, MessageHandler);
            messageProducer.Send(destination, MessageBuilder.WithPayload(payload).Build());
            IMessage message;
            queue.TryTake(out message, TimeSpan.FromSeconds(10));

            Assert.IsNotNull(message);
            Assert.AreEqual(payload, message.Payload);

        }

        #region References
        public void MessageHandler(IMessage message, IServiceProvider serviceProvider)
        {
            queue.Add((IO.Eventuate.Tram.Messaging.Common.Message)message);
        }
        #endregion
    }
}
