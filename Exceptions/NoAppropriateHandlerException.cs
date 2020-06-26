using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace RabbitMQHelper.Exceptions
{
    public class NoAppropriateHandlerException : Exception
    {
        public NoAppropriateHandlerException()
        {
        }

        public NoAppropriateHandlerException(string message) : base(message)
        {
        }

        public NoAppropriateHandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoAppropriateHandlerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
