using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQHelper.Exceptions;
using RabbitMQHelper.Interfaces;
using RabbitMQHelper.Models;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQHelper
{
    public class RabbitMessageProducer : IRabbitMessageProducer
    {
        private readonly ILogger<RabbitMessageProducer> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueueName;
        private readonly EventingBasicConsumer _consumer;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper =
                new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public RabbitMessageProducer(ILogger<RabbitMessageProducer> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            var factory = new ConnectionFactory() { Uri = new Uri(configuration.GetSection("RabbitMqConnection").Value) };

            _logger.LogInformation($"{nameof(RabbitMessageProducer)}-Creating connection to rabbit using: {factory.Uri}");
            _connection = factory.CreateConnection();
            _logger.LogInformation($"{nameof(RabbitMessageProducer)}-Connection created successfully.");

            _channel = _connection.CreateModel();

            _replyQueueName = _channel.QueueDeclare().QueueName;
            _logger.LogInformation($"{nameof(RabbitMessageProducer)}-Reply queue created: {_replyQueueName}");

            _consumer = new EventingBasicConsumer(_channel);

            _consumer.Received += (model, ea) =>
            {
                if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId,
                    out var taskCompletionSource))
                    return;

                var body = ea.Body;
                var response = Encoding.UTF8.GetString(body);

                taskCompletionSource.TrySetResult(response);
            };

            _channel.BasicConsume(consumer: _consumer, queue: _replyQueueName, autoAck: true);

            _logger.LogInformation($"{nameof(RabbitMessageProducer)}-Consumer started, listening for messages");
        }

        public async Task<T> PublishAndGetResponseAsync<T>(string exchage, string routingKey, string method, object dataModel)
        {
            var request = new RabbitMessageRequestModel()
            {
                Method = method,
                Data = dataModel
            };

            var requestJson = JsonConvert.SerializeObject(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);

            var properties = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();

            properties.CorrelationId = correlationId;
            properties.ReplyTo = _replyQueueName;

            var taskCompletionSource = new TaskCompletionSource<string>();
            _callbackMapper.TryAdd(correlationId, taskCompletionSource);

            _channel.BasicPublish(
                  exchange: exchage,
                  routingKey: routingKey,
                  basicProperties: properties,
                  body: requestBytes);

            var response = await taskCompletionSource.Task;

            _logger.LogInformation($"{nameof(RabbitMessageProducer)}.{nameof(PublishAndGetResponseAsync)}-Message returned");

            var responseObject = JsonConvert.DeserializeObject<RabbitMessageResponseModel>(response);

            if (!responseObject.HandledSuccessfully) throw new NoAppropriateHandlerException("No appropriate handler found.");

            if (responseObject.ServerThrownError) throw new RPCServerThrewErrorException(responseObject.Message);

            responseObject.Data = JsonConvert.DeserializeObject<T>(responseObject.Data.ToString());

            return (T)responseObject.Data;
        }
    }
}
