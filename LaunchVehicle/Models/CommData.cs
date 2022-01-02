using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchVehicle
{
    public sealed class CommData
    {
        public string Id { get; }
        public double Uplink { get; }
        public double Downlink { get; }
        public double ActiveTransponders { get; }
        public DateTime CreatedDateTime { get; }

        public CommData(string id, double uplink, double downlink, double activetransponders, DateTime createdDateTime)
        {
            Id = id;
            Uplink = uplink;
            Downlink = downlink;
            ActiveTransponders = activetransponders;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
