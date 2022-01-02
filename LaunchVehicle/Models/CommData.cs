using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchVehicle
{
    public sealed class CommData
    {
        public string Type { get; }
        public string Id { get; }
        public int LvId { get; }
        public double Uplink { get; }
        public double Downlink { get; }
        public double ActiveTransponders { get; }
        public DateTime CreatedDateTime { get; }

        public CommData(string type, string id, int lvId, double uplink, double downlink, double activetransponders, DateTime createdDateTime)
        {
            Type = type;
            Id = id;
            LvId = lvId;
            Uplink = uplink;
            Downlink = downlink;
            ActiveTransponders = activetransponders;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
