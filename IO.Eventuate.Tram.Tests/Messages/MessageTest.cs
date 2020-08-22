using System;
using System.Collections.Generic;
using IO.Eventuate.Tram.Messaging.Producer;
using IO.Eventuate.Tram.Consumer.Kafka;
using IO.Eventuate.Tram.Messaging.Common;
using System.Linq;
using IO.Eventuate.Tram.Messaging.Producer.Database;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Consumer.Common;
using IO.Eventuate.Tram.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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
            })
            .AddSingleton<IEnumerable<IMessageInterceptor>>(new List<IMessageInterceptor>());

            serviceCollection.AddDbContext<EventuateTramDbContext>((provider, o) =>
            {
                o.UseSqlServer(TestSettings.EventuateTramDbConnection)
                    //.ReplaceService<IModelCacheKeyFactory, DynamicEventuateSchemaModelCacheKeyFactory>()
                    ;
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

        public class EventuateTramDbContextProvider : IEventuateTramDbContextProvider
        {
            private readonly EventuateSchema _eventuateSchema;
            private readonly DbContextOptions<EventuateTramDbContext> _dbContextOptions;

            public EventuateTramDbContextProvider(DbContextOptions<EventuateTramDbContext> dbContextOptions,
                EventuateSchema eventuateSchema)
            {
                _eventuateSchema = eventuateSchema;
                _dbContextOptions = dbContextOptions;
            }

            public EventuateTramDbContext CreateDbContext()
            {
                return new EventuateTramDbContext(_dbContextOptions, _eventuateSchema);
            }
        }

        public class IdGenerator : IIdGenerator
        {
            public IdGenerator()
            {
            }
            public Int128 GenId()
            {
                return new Int128(System.DateTime.Now.Ticks, System.DateTime.Now.Ticks - 20);
            }
        }

        #endregion

    }
}
