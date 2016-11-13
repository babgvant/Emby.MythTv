using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Model
{
    public class Channel
    {
        public string ChanId { get; set; }
        public string ChanNum { get; set; }
        public string CallSign { get; set; }
        public string IconURL { get; set; }
        public string ChannelName { get; set; }
        public string MplexId { get; set; }
        public string ServiceId { get; set; }
        public string ATSCMajorChan { get; set; }
        public string ATSCMinorChan { get; set; }
        public string Format { get; set; }
        public string FrequencyId { get; set; }
        public string FineTune { get; set; }
        public string ChanFilters { get; set; }
        public string SourceId { get; set; }
        public string InputId { get; set; }
        public string CommFree { get; set; }
        public string UseEIT { get; set; }
        public string Visible { get; set; }
        public string XMLTVID { get; set; }
        public string DefaultAuth { get; set; }
        public List<Program> Programs { get; set; }
    }
}
