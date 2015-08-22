using babgvant.Emby.MythTv.Helpers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Responses
{
    public class CaptureResponse
    {
        public static CaptureCardList ParseCaptureCardList(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootCaptureObject>(stream);
            UtilsHelper.DebugInformation(logger, string.Format("[MythTV] ParseCaptureCardList Response: {0}", json.SerializeToString(root)));
            return root.CaptureCardList;
        }
    }

    public class CaptureCard
    {
        public string CardId { get; set; }
        public string VideoDevice { get; set; }
        public string AudioDevice { get; set; }
        public string VBIDevice { get; set; }
        public string CardType { get; set; }
        public string AudioRateLimit { get; set; }
        public string HostName { get; set; }
        public string DVBSWFilter { get; set; }
        public string DVBSatType { get; set; }
        public string DVBWaitForSeqStart { get; set; }
        public string SkipBTAudio { get; set; }
        public string DVBOnDemand { get; set; }
        public string DVBDiSEqCType { get; set; }
        public string FirewireSpeed { get; set; }
        public string FirewireModel { get; set; }
        public string FirewireConnection { get; set; }
        public string SignalTimeout { get; set; }
        public string ChannelTimeout { get; set; }
        public string DVBTuningDelay { get; set; }
        public string Contrast { get; set; }
        public string Brightness { get; set; }
        public string Colour { get; set; }
        public string Hue { get; set; }
        public string DiSEqCId { get; set; }
        public string DVBEITScan { get; set; }
    }

    public class CaptureCardList
    {
        public List<CaptureCard> CaptureCards { get; set; }
    }

    public class RootCaptureObject
    {
        public CaptureCardList CaptureCardList { get; set; }
    }
}
