using System;

namespace LaunchVehicle
{
    internal sealed class Message
    {
        public Byte[] Body { get; } // Serialized message
        public string MessageId { get; }
        public string ContentType { get; }

        public Message(byte[] body, string messageId, string contentType)
        {
            Body = body;
            MessageId = messageId;
            ContentType = contentType;
        }
    }
}