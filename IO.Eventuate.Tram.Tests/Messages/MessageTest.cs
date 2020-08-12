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
using System.Threading.Channels;

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
            //Producer
            IdGenerator generator = new IdGenerator();
            var options = new DbContextOptionsBuilder<EventuateTramDbContext>().UseSqlServer(TestSettings.EventuateTramDbConnection).Options;
            var schema = new EventuateSchema(TestSettings.EventuateTramDbSchema);
            EventuateTramDbContextProvider provider = new EventuateTramDbContextProvider(options, schema);
            messageProducer = new DatabaseMessageProducer(new List<IMessageInterceptor>(), generator, provider, Substitute.For<ILogger<DatabaseMessageProducer>>());

            //Consumer
            PrePostHandlerMessageHandlerDecorator prepostmessageHandlerDecorator = new PrePostHandlerMessageHandlerDecorator();
            IList<IMessageHandlerDecorator> decorators = new List<IMessageHandlerDecorator>();
            decorators.Add((IMessageHandlerDecorator)prepostmessageHandlerDecorator);
            messageConsumer = new KafkaMessageConsumer(TestSettings.KafkaBootstrapServers,
                EventuateKafkaConsumerConfigurationProperties.Empty(),
                new DecoratedMessageHandlerFactory(decorators,
                Substitute.For<ILogger<DecoratedMessageHandlerFactory>>()),
                Substitute.For<ILoggerFactory>(),
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
        public void Accept(SubscriberIdAndMessage subscriberIdAndMessage, IServiceProvider serviceProvider, IMessageHandlerDecoratorChain chain)
        {

        }
        public class PrePostHandlerMessageHandlerDecorator : IMessageHandlerDecorator
        {
            public Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept =>
                (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
                {
                    IMessage message = subscriberIdAndMessage.Message;
                    string subscriberId = subscriberIdAndMessage.SubscriberId;
                    IMessageInterceptor[] messageInterceptors =
                        serviceProvider.GetServices<IMessageInterceptor>().ToArray();
                    PreHandle(subscriberId, message, messageInterceptors);
                    try
                    {
                        messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage, serviceProvider);
                        PostHandle(subscriberId, message, messageInterceptors, null);
                    }
                    catch (Exception e)
                    {
                        PostHandle(subscriberId, message, messageInterceptors, e);
                        throw;
                    }
                };

            private void PreHandle(string subscriberId, IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
            {
                foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
                {
                    messageInterceptor.PreHandle(subscriberId, message);
                }
            }


            private void PostHandle(string subscriberId, IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors, Exception e)
            {
                foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
                {
                    messageInterceptor.PostHandle(subscriberId, message, e);
                }
            }
            public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostHandlerMessageHandlerDecorator;
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
