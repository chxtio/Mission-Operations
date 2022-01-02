using System;

namespace LaunchVehicle
{
    public sealed class SciData
    {
        public string Id { get; }
        public double Rainfall { get; }
        public double Humidity { get; }
        public double Snow { get; }
        public DateTime CreatedDateTime { get; }

        public SciData(string id, double rainfall, double humidity, double snow, DateTime createdDateTime)
        {
            Id = id;
            Rainfall = rainfall;
            Humidity = humidity;
            Snow = snow;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
