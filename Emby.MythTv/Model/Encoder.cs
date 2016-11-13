using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Model
{
    public class Encoder
    {
        public string Id { get; set; }
        public string HostName { get; set; }
        public string Local { get; set; }
        public string Connected { get; set; }
        public int State { get; set; }
        public string SleepStatus { get; set; }
        public string LowOnFreeSpace { get; set; }
        public List<Input> Inputs { get; set; }
        public Recording Recording { get; set; }
    }
}
