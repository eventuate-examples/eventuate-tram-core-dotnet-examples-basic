using System;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace IO.Eventuate.Tram.Tests.TestHelpers
{
    public class TestServicesHost
    {
        public string _aggregateType = "test";
        public void SetAggregateType(string aggregateType)
        {
            _aggregateType = aggregateType;
        }
        public IHost SetUpHost()
        {
            var _host = new HostBuilder()
           .ConfigureServices((hostContext, services) =>
           {
               // Logging 
               services.AddLogging(builder =>
               {
                   builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                   builder.AddConsole();
                   builder.AddDebug();
               });
               // DBContext
               services.AddDbContext<EventuateTramDbContext>((provider, o) =>
               {
                   o.UseSqlServer(TestSettings.EventuateTramDbConnection);
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
                   return consumer.DomainEventHandlers(_aggregateType);
               });
           }).Build();
            _host.StartAsync().Wait();
            return _host;
        }
    }
}
