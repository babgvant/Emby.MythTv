using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class RecRuleList
    {
        public string StartIndex { get; set; }
        public string Count { get; set; }
        public string TotalAvailable { get; set; }
        public string AsOf { get; set; }
        public string Version { get; set; }
        public string ProtoVer { get; set; }
        public List<RecRule> RecRules { get; set; }
    }
}
