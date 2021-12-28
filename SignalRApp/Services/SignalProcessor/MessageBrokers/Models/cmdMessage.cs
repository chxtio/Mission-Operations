using System;

namespace SignalRApp
{
    public sealed class cmdMessage
    {
        public string Id { get; }
        public string Type { get; }
        public string Cmd { get; }
        public string Target { get; }
        public DateTime CreatedDateTime { get; }

        public cmdMessage(string id, string type, string cmd, string target, DateTime createdDateTime)
        {
            Id = id;
            Type = type;
            Cmd = cmd;
            Target = target;
            CreatedDateTime = createdDateTime;
        }
    }
}