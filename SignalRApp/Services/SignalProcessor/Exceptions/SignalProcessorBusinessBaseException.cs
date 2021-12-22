using System;
using System.Diagnostics.CodeAnalysis;

namespace SignalRApp
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public abstract class SignalProcessorBusinessBaseException : SignalProcessorBaseException
    {
        protected SignalProcessorBusinessBaseException() { }
        protected SignalProcessorBusinessBaseException(string message) : base(message) { }
        protected SignalProcessorBusinessBaseException(string message, Exception inner) : base(message, inner) { }
        protected SignalProcessorBusinessBaseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
