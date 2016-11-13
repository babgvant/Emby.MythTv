using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.LiveTv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using babgvant.Emby.MythTv.Helpers;

namespace babgvant.Emby.MythTv.Responses
{
    public class ChannelResponse
    {
	private static readonly CultureInfo _usCulture = new CultureInfo("en-US");
	
        public static IEnumerable<string> GetVideoSourceList(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootVideoSourceObject>(stream);
            return root.VideoSourceList.VideoSources.Select(i => i.Id);
        }

	public static IEnumerable<ChannelInfo> GetChannels(Stream stream, IJsonSerializer json, ILogger logger,
							   bool loadChannelIcons)
	{
	    var root = json.DeserializeFromStream<RootChannelInfoListObject>(stream).ChannelInfoList.ChannelInfos;
	    UtilsHelper.DebugInformation(logger, string.Format("[MythTV] GetChannels Response: {0}",
							       json.SerializeToString(root)));
	    return root.Select(x => GetChannel(x, loadChannelIcons));
	}

	private static ChannelInfo GetChannel(Channel channel, bool loadChannelIcons)
	{
	    ChannelInfo ci = new ChannelInfo()
		{
		    Name = channel.ChannelName,
		    Number = channel.ChanNum,
		    Id = channel.ChanId.ToString(_usCulture),
		    HasImage = false
		};

	    if (!string.IsNullOrWhiteSpace(channel.IconURL) && loadChannelIcons)
	    {
		ci.HasImage = true;
		ci.ImageUrl = string.Format("{0}/Guide/GetChannelIcon?ChanId={1}", Plugin.Instance.Configuration.WebServiceUrl, channel.ChanId);
	    }

	    return ci;
	}

	private class VideoSource
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

	private class VideoSourceList
	{
	    public DateTime? AsOf { get; set; }
	    public string Version { get; set; }
	    public string ProtoVer { get; set; }
	    public List<VideoSource> VideoSources { get; set; }
	}

	private class RootVideoSourceObject
	{
	    public VideoSourceList VideoSourceList { get; set; }
	}

    	private class RecordingDetail
	{
	    public string Status { get; set; }
	    public string Priority { get; set; }
	    public string StartTs { get; set; }
	    public string EndTs { get; set; }
	    public string RecordId { get; set; }
	    public string RecGroup { get; set; }
	    public string PlayGroup { get; set; }
	    public string StorageGroup { get; set; }
	    public string RecType { get; set; }
	    public string DupInType { get; set; }
	    public string DupMethod { get; set; }
	    public string EncoderId { get; set; }
	    public string Profile { get; set; }
	}

	private class ArtworkInfo
	{
	    public string URL { get; set; }
	    public string FileName { get; set; }
	    public string StorageGroup { get; set; }
	    public string Type { get; set; }
	}

	private class Artwork
	{
	    public List<ArtworkInfo> ArtworkInfos { get; set; }
	}

	private class Program
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

	private class Channel
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

	private class ChannelInfoList
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

	private class RootChannelInfoListObject
	{
	    public ChannelInfoList ChannelInfoList { get; set; }
	}
    }
}
