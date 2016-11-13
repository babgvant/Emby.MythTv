using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using babgvant.Emby.MythTv.Helpers;

namespace babgvant.Emby.MythTv.Responses
{
    public class GuideResponse
    {

	RootObject root;
	
	public GuideResponse(Stream stream, IJsonSerializer json)
	{
            root = json.DeserializeFromStream<RootObject>(stream);
	}
	
        public IEnumerable<ProgramInfo> GetPrograms(string channelId, ILogger logger)
        {
	    var listings = root.ProgramGuide.Channels;
	    return listings.Where(i => string.Equals(i.ChanId.ToString(), channelId))
		.SelectMany(i => i.Programs.Select(e => GetProgram(channelId, e)));
        }

	private ProgramInfo GetProgram(string channelId, Program prog)
	{
	    var info = new ProgramInfo()
		{
		    Name = prog.Title,
		    EpisodeTitle = prog.SubTitle,
		    Overview = prog.Description,
		    Audio = ProgramAudio.Stereo, //Hardcode for now (ProgramAudio)item.AudioProps,
		    ChannelId = channelId,
		    EndDate = (DateTime)prog.EndTime,
		    StartDate = (DateTime)prog.StartTime,
		    Id = string.Format("StartTime={0}&ChanId={1}", ((DateTime)prog.StartTime).Ticks, channelId),
		    IsSeries = GeneralHelpers.ContainsWord(prog.CatType, "series", StringComparison.OrdinalIgnoreCase),
		    IsMovie = GeneralHelpers.ContainsWord(prog.CatType, "movie", StringComparison.OrdinalIgnoreCase),
		    IsRepeat = prog.Repeat,
		    IsNews = GeneralHelpers.ContainsWord(prog.Category, "news",
							 StringComparison.OrdinalIgnoreCase),
		    IsKids = GeneralHelpers.ContainsWord(prog.Category, "animation",
							 StringComparison.OrdinalIgnoreCase),
		    IsSports =
		    GeneralHelpers.ContainsWord(prog.Category, "sport",
						StringComparison.OrdinalIgnoreCase) ||
		    GeneralHelpers.ContainsWord(prog.Category, "motor sports",
						StringComparison.OrdinalIgnoreCase) ||
		    GeneralHelpers.ContainsWord(prog.Category, "football",
						StringComparison.OrdinalIgnoreCase) ||
		    GeneralHelpers.ContainsWord(prog.Category, "cricket",
						StringComparison.OrdinalIgnoreCase)
		};
	    
	    return info;
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

	private class ProgramGuide
	{
	    public string StartTime { get; set; }
	    public string EndTime { get; set; }
	    public string Details { get; set; }
	    public string StartIndex { get; set; }
	    public string Count { get; set; }
	    public string TotalAvailable { get; set; }
	    public string AsOf { get; set; }
	    public string Version { get; set; }
	    public string ProtoVer { get; set; }
	    public List<Channel> Channels { get; set; }
	}

	private class RootObject
	{
	    public ProgramGuide ProgramGuide { get; set; }
	}

    }
}
