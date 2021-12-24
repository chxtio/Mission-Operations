using System;

namespace LaunchVehicle
{
    public sealed class TeleMessage
    {
        public string Id { get; }
        public double Altitude { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        public double Temperature { get; }
        public double TimeToOrbit { get; }
        public DateTime CreatedDateTime { get; }

        public TeleMessage(string id, double altitude, double longitude, double latitude, double temperature, double timeToOrbit, DateTime createdDateTime)
        {
            Id = id;
            Altitude = altitude;
            Longitude = longitude;
            Latitude = latitude;
            Temperature = temperature;
            TimeToOrbit = timeToOrbit;
            CreatedDateTime = createdDateTime;
        }
    }
}