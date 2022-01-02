using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchVehicle
{
    public sealed class SpyData
    {
        public string Type { get; }
        public string Id { get; }
        public int LvId { get; }
        public string ImgUrl { get; }
        public DateTime CreatedDateTime { get; }

        public SpyData(string type, string id, int lvId, string imgurl, DateTime createdDateTime)
        {
            Type = type;
            Id = id;
            LvId = lvId;
            ImgUrl = imgurl;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
