using RabbitMQHelper.Models;
using System;
using System.Threading.Tasks;

namespace RabbitMQHelper
{
    public abstract class RabbitMessageHandler
    {
        protected abstract string MethodCanHandle { get; }

        public bool CanHandle(string method)
        {
            return string.Equals(MethodCanHandle, method, StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task<RabbitMessageResponseModel> CallHandleMessage(RabbitMessageRequestModel messageRequest)
        {
            try
            {
                var responseData = await ConvertMessageAndHandle(messageRequest);

                return new RabbitMessageResponseModel()
                {
                    Data = responseData
                };
            }
            catch (Exception ex)
            {
                return new RabbitMessageResponseModel()
                {
                    ServerThrownError = true,
                    Message = ex.Message
                };
            }
        }

        protected abstract Task<object> ConvertMessageAndHandle(RabbitMessageRequestModel messageRequest);
    }
}
