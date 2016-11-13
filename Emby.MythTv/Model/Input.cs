using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Model
{
    public class Input
    {
        public string Id { get; set; }
        public string CardId { get; set; }
        public string SourceId { get; set; }
        public string InputName { get; set; }
        public string DisplayName { get; set; }
        public string QuickTune { get; set; }
        public string RecPriority { get; set; }
        public string ScheduleOrder { get; set; }
        public string LiveTVOrder { get; set; }
    }
}
