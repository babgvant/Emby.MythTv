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
    public class GuideResponse
    {
        public static ProgramGuide GetPrograms(Stream stream, IJsonSerializer json, string channelId, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootProgramGuideObject>(stream);
            return root.ProgramGuide;
        }
    }

    public class Program
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Category { get; set; }
        public string CatType { get; set; }
        public bool Repeat { get; set; }
        public string VideoProps { get; set; }
        public string AudioProps { get; set; }
        public string SubProps { get; set; }
        public string SeriesId { get; set; }
        public string ProgramId { get; set; }
        public string Stars { get; set; }
        public string FileSize { get; set; }
        public string LastModified { get; set; }
        public string ProgramFlags { get; set; }
        public string FileName { get; set; }
        public string HostName { get; set; }
        public DateTime? Airdate { get; set; }
        public string Description { get; set; }
        public string Inetref { get; set; }
        public string Season { get; set; }
        public string Episode { get; set; }
        public Channel Channel { get; set; }
        public RecordingDetail Recording { get; set; }
        public Artwork Artwork { get; set; }
    }

    public class ProgramGuide
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string StartChanId { get; set; }
        public string EndChanId { get; set; }
        public string NumOfChannels { get; set; }
        public string Details { get; set; }
        public string Count { get; set; }
        public string AsOf { get; set; }
        public string Version { get; set; }
        public string ProtoVer { get; set; }
        public List<Channel> Channels { get; set; }
    }

    public class RootProgramGuideObject
    {
        public ProgramGuide ProgramGuide { get; set; }
    }
}
