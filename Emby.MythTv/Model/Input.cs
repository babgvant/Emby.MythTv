using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class Input
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public int SourceId { get; set; }
        public int MplexId { get; set; }
        public string InputName { get; set; }
        public string DisplayName { get; set; }
        public bool QuickTune { get; set; }
        public int RecPriority { get; set; }
        public int ScheduleOrder { get; set; }
        public int LiveTVOrder { get; set; }
    }
}
