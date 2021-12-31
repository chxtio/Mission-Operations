using System;

namespace LaunchVehicle
{
    public sealed class LaunchMessage
    {
        public string Type { get; }
        public string Id { get; }
        public int LvId { get; }
        public string Status { get; }

        public DateTime CreatedDateTime { get; }

        public LaunchMessage(string type, string id, int lvId, string status, DateTime createdDateTime)
        {
            Type = type;
            Id = id;
            LvId = lvId;
            Status = status;
            CreatedDateTime = createdDateTime;
        }
    }
}