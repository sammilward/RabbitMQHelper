using System.Threading.Tasks;

namespace RabbitMQHelper.Interfaces
{
    public interface IRabbitMessageProducer
    {
        Task<T> PublishAndGetResponseAsync<T>(string exchage, string routingKey, string method, object dataModel);
    }
}
