using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class ChannelInfoList
    {
        public string StartIndex { get; set; }
        public int Count { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalAvailable { get; set; }
        public DateTime? AsOf { get; set; }
        public string Version { get; set; }
        public string ProtoVer { get; set; }
        public List<Channel> ChannelInfos { get; set; }
    }
}
