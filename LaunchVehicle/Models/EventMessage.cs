using System;

namespace LaunchVehicle
{
    public sealed class EventMessage
    {
        public string ID { get; }
        public string Title { get; }
        public string Test { get; }
        public DateTime CreatedDateTime { get; }

        public EventMessage(string id, string title, string test, DateTime createdDateTime)
        {
            ID = id;
            Title = title;
            Test = test;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
