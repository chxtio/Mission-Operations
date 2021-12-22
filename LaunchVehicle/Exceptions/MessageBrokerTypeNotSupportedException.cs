using System;
using System.Diagnostics.CodeAnalysis;

namespace LaunchVehicle
{
    [ExcludeFromCodeCoverage]

    public sealed class MessageBrokerTypeNotSupportedException : Exception
    {
        public MessageBrokerTypeNotSupportedException() { }
        public MessageBrokerTypeNotSupportedException(string message) : base(message) { }
        public MessageBrokerTypeNotSupportedException(string message, Exception inner) : base(message, inner) { }
    }
}
