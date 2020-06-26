using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQHelper.Models;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQHelper
{
    public class RabbitConsumorService : IHostedService
    {
        private readonly ILogger<RabbitConsumorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitConsumorService(ILogger<RabbitConsumorService> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_configuration.GetSection("RabbitMqConnection").Value)
            };

            try
            {
                _logger.LogInformation($"{nameof(RabbitConsumorService)} - Connecting to rabbit: {factory.Uri})");
                _connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{nameof(RabbitConsumorService)} - Failed to connect to rabbit. {ex.Message}");
                throw;
            }

            _channel = _connection.CreateModel();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Register();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _connection.Close();
            return Task.CompletedTask;
        }

        public void Register()
        {
            var queueName = _configuration.GetSection("RabbitMqQueueName").Value;
            var exchangeName = _configuration.GetSection("RabbitMqExchangeName").Value;
            var routingKey = _configuration.GetSection("RabbitMqRoutingKey").Value; 

            _channel.ExchangeDeclare(exchangeName, type: "topic");
            _channel.QueueDeclare(queueName, exclusive: false);
            _channel.QueueBind(queueName, exchangeName, routingKey);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body;
                var properties = ea.BasicProperties;
                var replyProperties = _channel.CreateBasicProperties();
                replyProperties.CorrelationId = properties.CorrelationId;

                _logger.LogInformation($"Recieved message: {ea.BasicProperties.CorrelationId}");

                RabbitMessageResponseModel response = null;

                try
                {
                    var bodyString = Encoding.UTF8.GetString(body);
                    var requestModel = JsonConvert.DeserializeObject<RabbitMessageRequestModel>(bodyString);

                    response = await ProcessMessage(requestModel);
                    response.Message = "Handled correctly";
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{nameof(RabbitConsumorService)}.{nameof(Register)}-{ex.Message}");

                    response = new RabbitMessageResponseModel()
                    {
                        HandledSuccessfully = false,
                        Message = ex.Message
                    };
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));

                    _channel.BasicPublish(exchange: "", routingKey: properties.ReplyTo, basicProperties: replyProperties, body: responseBytes);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            _channel.BasicConsume(queue: queueName, consumer: consumer);
        }

        public async Task<RabbitMessageResponseModel> ProcessMessage(RabbitMessageRequestModel requestModel)
        {
            try
            {
                RabbitMessageResponseModel rabbitMessageResponseModel;

                using (var scope = _serviceProvider.CreateScope())
                {
                    _logger.LogInformation($"{nameof(RabbitConsumorService)}:{nameof(ProcessMessage)}-" +
                                           $"Getting instance of: {nameof(RabbitMessageDelegator)}");

                    var rabbitMessageDelegator = scope.ServiceProvider.GetRequiredService<RabbitMessageDelegator>();

                    _logger.LogInformation($"{nameof(RabbitConsumorService)}:{nameof(ProcessMessage)}-" +
                                           $"Finding suitable handler for message");

                    var messageHandler = rabbitMessageDelegator.FindAppropriateMessageHandler(requestModel);

                    _logger.LogInformation($"{nameof(RabbitConsumorService)}:{nameof(ProcessMessage)}-" +
                                           $"Sending message to chosen handler");

                    rabbitMessageResponseModel = await messageHandler.CallHandleMessage(requestModel);
                }

                _logger.LogInformation($"{nameof(RabbitConsumorService)}:{nameof(ProcessMessage)}-" +
                                       $"Message handled");

                return rabbitMessageResponseModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(RabbitConsumorService)}:{nameof(ProcessMessage)}-" +
                                       $"{ex.Message}");

                throw;
            }
        }
    }
}
