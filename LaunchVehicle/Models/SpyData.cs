using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchVehicle
{
    public sealed class SpyData
    {
        public string Id { get; }
        public string ImgUrl { get; }
        public DateTime CreatedDateTime { get; }

        public SpyData(string id, string imgurl, DateTime createdDateTime)
        {
            Id = id;
            ImgUrl = imgurl;
            CreatedDateTime = CreatedDateTime;
        }
    }
}
