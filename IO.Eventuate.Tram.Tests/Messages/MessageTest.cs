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
            var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddConsole();
                builder.AddDebug();
            })
            .BuildServiceProvider();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var loggerProd = loggerFactory.CreateLogger<DatabaseMessageProducer>();
            var loggerCons = loggerFactory.CreateLogger<DecoratedMessageHandlerFactory>();
            //Producer
            IdGenerator generator = new IdGenerator();
            var options = new DbContextOptionsBuilder<EventuateTramDbContext>().UseSqlServer(TestSettings.EventuateTramDbConnection).Options;
            var schema = new EventuateSchema(TestSettings.EventuateTramDbSchema);
            EventuateTramDbContextProvider provider = new EventuateTramDbContextProvider(options, schema);
            messageProducer = new DatabaseMessageProducer(new List<IMessageInterceptor>(), generator, provider, loggerProd);

            //Consumer
            IList<IMessageHandlerDecorator> decorators = new List<IMessageHandlerDecorator>();
            decorators.Add(Substitute.For<IMessageHandlerDecorator>());
            messageConsumer = new KafkaMessageConsumer(TestSettings.KafkaBootstrapServers,
                EventuateKafkaConsumerConfigurationProperties.Empty(),
                new DecoratedMessageHandlerFactory(decorators, loggerCons),
                loggerFactory,
                Substitute.For<IServiceScopeFactory>());

        }
        [TestMethod]
        public void ShouldReceiveMessage()
        {
            messageConsumer.Subscribe(subscriberId, channels, MessageHandler);
            messageProducer.Send(destination, MessageBuilder.WithPayload(payload).Build());
            IMessage message;
            queue.TryTake(out message, TimeSpan.FromSeconds(10));

            Assert.IsNotNull(message);
            Assert.Equals(payload, message.Payload);

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
