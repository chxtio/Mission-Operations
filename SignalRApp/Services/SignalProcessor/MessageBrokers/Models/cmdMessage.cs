using System;

namespace SignalRApp
{
    public sealed class cmdMessage
    {
        public string Id { get; }
        public string Cmd { get; }
        public string Target { get; }
        public DateTime CreatedDateTime { get; }

        public cmdMessage(string id, string cmd, string target, DateTime createdDateTime)
        {
            Id = id;
            Cmd = cmd;
            Target = target;
            CreatedDateTime = createdDateTime;
        }
    }
}