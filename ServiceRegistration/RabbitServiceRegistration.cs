using Microsoft.Extensions.DependencyInjection;
using RabbitMQHelper.Interfaces;
using System;
using System.Linq;

namespace RabbitMQHelper
{
    public static class RabbitServiceRegistration
    {
        public static void RegisterConsumorService(IServiceCollection services)
        {
            services.AddScoped<RabbitMessageDelegator>();

            var handlerType = typeof(RabbitMessageHandler);

            foreach (var handlers in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => handlerType.IsAssignableFrom(x) && x != handlerType).ToList())
            {
                services.AddScoped(typeof(RabbitMessageHandler), handlers);
            }

            services.AddHostedService<RabbitConsumorService>();
        }

        public static void RegisterProducerService(IServiceCollection serivces)
        {
            serivces.AddSingleton<IRabbitMessageProducer, RabbitMessageProducer>();
        }
    }
}
