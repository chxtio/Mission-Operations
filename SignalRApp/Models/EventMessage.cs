using System;

namespace SignalRApp
{
    public class EventMessage
    {
        //public string ID { get; }
        //public string Title { get; }
        //public DateTime CreatedDateTime { get; }

        //public EventMessage(string id, string title, DateTime createdDateTime)
        //{
        //    ID = id;
        //    Title = title;
        //    CreatedDateTime = CreatedDateTime;
        //}

        public string ID { get; set; }
        public string Title { get; set; }
        public DateTime CreatedDateTime { get; set; }

        // default constructor, deserialize messages from message broker to sent to browser
        public EventMessage()
        { }

        public EventMessage(string id, string title, DateTime createdDateTime)
        {
            ID = id;
            Title = title;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
