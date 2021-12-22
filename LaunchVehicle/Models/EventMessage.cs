using System;

namespace LaunchVehicle
{
    public sealed class EventMessage
    {
        public string ID { get;}
        public string Title { get; }
        public DateTime CreatedDateTime { get; }

        public EventMessage(string id, string title, DateTime createdDateTime)
        {
            ID = id;
            Title = title;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
