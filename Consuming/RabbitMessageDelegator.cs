using Microsoft.Extensions.Logging;
using RabbitMQHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMQHelper
{
    public class RabbitMessageDelegator
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<RabbitMessageHandler> _messageHandlers;

        public RabbitMessageDelegator(ILogger<RabbitMessageDelegator> logger, IEnumerable<RabbitMessageHandler> messageHandlers)
        {
            _logger = logger;
            _messageHandlers = messageHandlers;
        }

        public RabbitMessageHandler FindAppropriateMessageHandler(RabbitMessageRequestModel rabbitMessageRequest)
        {
            try
            {
                _logger.LogInformation($"Finding message handler for method: {rabbitMessageRequest.Method}");

                var suitableHandler = _messageHandlers.First(x => x.CanHandle(rabbitMessageRequest.Method));

                _logger.LogInformation($"Found message handler: {suitableHandler.GetType().Name}");

                return suitableHandler;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"No suitable message handler for method: {rabbitMessageRequest.Method}");

                throw ex;
            }
        }
    }
}
