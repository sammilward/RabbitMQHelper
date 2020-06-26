using System;
using System.Runtime.Serialization;

namespace RabbitMQHelper.Exceptions
{
    public class RPCServerThrewErrorException : Exception
    {
        public RPCServerThrewErrorException()
        {
        }

        public RPCServerThrewErrorException(string message) : base(message)
        {
        }

        public RPCServerThrewErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RPCServerThrewErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
