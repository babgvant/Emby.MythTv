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
    public class ChannelResponse
    {
        public static VideoSourceList ParseVideoSourceList(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootVideoSourceObject>(stream);
            return root.VideoSourceList;
        }

        public static ChannelInfoList ParseChannelInfoList(Stream stream, IJsonSerializer json, ILogger logger)
        {
            try
            {
                var root = json.DeserializeFromStream<RootChannelInfoListObject>(stream);
                return root.ChannelInfoList;
            } catch(Exception ex)
            {
                logger.Error("ParseChannelInfoList: {0}", ex.Message);
                return null;
            }
        }
    }

    public class VideoSource
    {
        public string Id { get; set; }
        public string SourceName { get; set; }
        public string Grabber { get; set; }
        public string UserId { get; set; }
        public string FreqTable { get; set; }
        public string LineupId { get; set; }
        public bool Password { get; set; }
        public string UseEIT { get; set; }
        public string ConfigPath { get; set; }
        public string NITId { get; set; }
    }

    public class VideoSourceList
    {
        public DateTime? AsOf { get; set; }
        public string Version { get; set; }
        public string ProtoVer { get; set; }
        public List<VideoSource> VideoSources { get; set; }
    }

    public class RootVideoSourceObject
    {
        public VideoSourceList VideoSourceList { get; set; }
    }

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

    public class RootChannelInfoListObject
    {
        public ChannelInfoList ChannelInfoList { get; set; }
    }
}
