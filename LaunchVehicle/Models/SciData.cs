using System;

namespace LaunchVehicle
{
    public sealed class SciData
    {
        public string Type { get; }
        public string Id { get; }
        public int LvId { get; }
        public double Rainfall { get; }
        public double Humidity { get; }
        public double Snow { get; }
        public DateTime CreatedDateTime { get; }

        public SciData(string type, string id, int lvId, double rainfall, double humidity, double snow, DateTime createdDateTime)
        {
            Type = type;
            Id = id;
            LvId = lvId;
            Rainfall = rainfall;
            Humidity = humidity;
            Snow = snow;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
