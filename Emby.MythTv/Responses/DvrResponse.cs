﻿using babgvant.Emby.MythTv.Helpers;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Responses
{

    public class ExistingTimerException : Exception
    {
	public string id {get; private set;}

	public ExistingTimerException(string id)
	    : base($"Existing timer {id}")
	{
	    this.id = id;
	}
    }
    
    public class UpcomingResponse
    {

	public List<TimerInfo> GetUpcomingList(Stream stream, IJsonSerializer json, ILogger logger)
        {

            var root = json.DeserializeFromStream<RootObject>(stream);
	    return root.ProgramList.Programs.Select(i => ProgramToTimerInfo(i)).ToList();

	}

	private TimerInfo ProgramToTimerInfo(Program item) {

	    string id = $"{item.Channel.ChanId}_{((DateTime)item.StartTime).Ticks}";
	    
	    TimerInfo timer = new TimerInfo()
		{
		    ChannelId = item.Channel.ChanId,
		    ProgramId = id,
		    Name = item.Title,
		    Overview = item.Description,
		    StartDate = (DateTime)item.StartTime,
		    EndDate = (DateTime)item.EndTime,
		    Status = RecordingStatus.New,
		    SeasonNumber = item.Season,
		    EpisodeNumber = item.Episode,
		    EpisodeTitle = item.Title,
		    IsRepeat = item.Repeat
		};


	    // see https://code.mythtv.org/doxygen/recordingtypes_8h_source.html#l00022
	    if (item.Recording.RecType == 4)
	    {
		// Only add on SeriesTimerId if a "Record All" rule
		timer.SeriesTimerId = item.Recording.RecordId;

		// Also set a unique id for this instance
		timer.Id = id;
	    }
	    else
	    {
		// Use the mythtv rule ID for single recordings
		timer.Id = item.Recording.RecordId;
	    }

	    timer.PrePaddingSeconds = (int)(timer.StartDate - item.Recording.StartTs).TotalSeconds;
	    timer.PostPaddingSeconds = (int)(item.Recording.EndTs - timer.EndDate).TotalSeconds;

	    timer.IsPrePaddingRequired = timer.PrePaddingSeconds > 0;
	    timer.IsPostPaddingRequired = timer.PostPaddingSeconds > 0;

	    return timer;
        }

	public IEnumerable<RecordingInfo> GetRecordings(Stream stream, IJsonSerializer json, ILogger logger)
	{

	    var root = json.DeserializeFromStream<RootObject>(stream);
	    return root.ProgramList.Programs.Select(i => ProgramToRecordingInfo(i));

	}
	
	private RecordingInfo ProgramToRecordingInfo(Program item) {

	    RecordingInfo recInfo = new RecordingInfo()
		{
		    Name = item.Title,
		    EpisodeTitle = item.SubTitle,
		    Overview = item.Description,
		    Audio = ProgramAudio.Stereo, //Hardcode for now (ProgramAudio)item.AudioProps,
		    ChannelId = item.Channel.ChanId,
		    ProgramId = string.Format("{1}_{0}", ((DateTime)item.StartTime).Ticks, item.Channel.ChanId),
		    SeriesTimerId = item.Recording.RecordId,
		    EndDate = item.EndTime,
		    StartDate = item.StartTime,
		    Url = string.Format("{0}{1}",
					Plugin.Instance.Configuration.WebServiceUrl,
					string.Format("/Content/GetFile?StorageGroup={0}&FileName={1}",
						      item.Recording.StorageGroup, item.FileName)),
		    Id = item.Recording.RecordedId,
		    IsSeries = GeneralHelpers.ContainsWord(item.CatType, "series", StringComparison.OrdinalIgnoreCase),
		    IsMovie = GeneralHelpers.ContainsWord(item.CatType, "movie", StringComparison.OrdinalIgnoreCase),
		    IsRepeat = item.Repeat,
		    IsNews = GeneralHelpers.ContainsWord(item.Category, "news",
							 StringComparison.OrdinalIgnoreCase),
		    IsKids = GeneralHelpers.ContainsWord(item.Category, "animation",
							 StringComparison.OrdinalIgnoreCase),
		    IsSports =
		    GeneralHelpers.ContainsWord(item.Category, "sport",
						StringComparison.OrdinalIgnoreCase) ||
		    GeneralHelpers.ContainsWord(item.Category, "motor sports",
						StringComparison.OrdinalIgnoreCase) ||
		    GeneralHelpers.ContainsWord(item.Category, "football",
						StringComparison.OrdinalIgnoreCase) ||
		    GeneralHelpers.ContainsWord(item.Category, "cricket",
						StringComparison.OrdinalIgnoreCase),

		    ShowId = item.ProgramId,
		    
		};

	    if (Plugin.Instance.RecordingUncs.Count > 0)
	    {
		foreach (string unc in Plugin.Instance.RecordingUncs)
		{
		    string recPath = Path.Combine(unc, item.FileName);
		    if (File.Exists(recPath))
		    {
			recInfo.Path = recPath;
			break;
		    }
		}
	    }

	    recInfo.Genres.AddRange(item.Category.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));

	    if (item.Artwork.ArtworkInfos.Count > 0)
	    {
		var url = item.Artwork.ArtworkInfos.Where(i => i.Type.Equals("coverart")).First().URL;
		recInfo.ImageUrl = string.Format("{0}{1}",
						 Plugin.Instance.Configuration.WebServiceUrl,
						 url);
		recInfo.HasImage = true;
	    }
	    else
		recInfo.HasImage = false;

	    return recInfo;

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
	    public List<object> Programs { get; set; }
	}

	private class Recording
	{
	    public string RecordedId { get; set; }
	    public string Status { get; set; }
	    public string Priority { get; set; }
	    public DateTime StartTs { get; set; }
	    public DateTime EndTs { get; set; }
	    public string FileSize { get; set; }
	    public string FileName { get; set; }
	    public string HostName { get; set; }
	    public string LastModified { get; set; }
	    public string RecordId { get; set; }
	    public string RecGroup { get; set; }
	    public string PlayGroup { get; set; }
	    public string StorageGroup { get; set; }
	    public int RecType { get; set; }
	    public string DupInType { get; set; }
	    public string DupMethod { get; set; }
	    public string EncoderId { get; set; }
	    public string EncoderName { get; set; }
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

	private class CastMember
	{
	    public string Name { get; set; }
	    public string CharacterName { get; set; }
	    public string Role { get; set; }
	    public string TranslatedRole { get; set; }
	}

	private class Cast
	{
	    public List<CastMember> CastMembers { get; set; }
	}

	private class Program
	{
	    public DateTime StartTime { get; set; }
	    public DateTime EndTime { get; set; }
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
	    public string LastModified { get; set; }
	    public string ProgramFlags { get; set; }
	    public string Airdate { get; set; }
	    public string Description { get; set; }
	    public string Inetref { get; set; }
	    public int? Season { get; set; }
	    public int? Episode { get; set; }
	    public string TotalEpisodes { get; set; }
	    public string FileSize { get; set; }
	    public string FileName { get; set; }
	    public string HostName { get; set; }
	    public Channel Channel { get; set; }
	    public Recording Recording { get; set; }
	    public Artwork Artwork { get; set; }
	    public Cast Cast { get; set; }
	}

	private class ProgramList
	{
	    public string StartIndex { get; set; }
	    public string Count { get; set; }
	    public string TotalAvailable { get; set; }
	    public string AsOf { get; set; }
	    public string Version { get; set; }
	    public string ProtoVer { get; set; }
	    public List<Program> Programs { get; set; }
	}

	private class RootObject
	{
	    public ProgramList ProgramList { get; set; }
	}

    }

    public class RuleResponse
    {
	public IEnumerable<SeriesTimerInfo> GetSeriesTimers(Stream stream, IJsonSerializer json, ILogger logger)
        {

            var root = json.DeserializeFromStream<RootObject>(stream);
	    return root.RecRuleList.RecRules
		.Where(rule => rule.Type.Equals("Record All"))
		.Select(i => RecRuleToSeriesTimerInfo(i));

	}

	private SeriesTimerInfo RecRuleToSeriesTimerInfo(RecRule item)
	{
	    var info = new SeriesTimerInfo()
		{
		    Name = item.Title,
		    ChannelId = item.ChanId,
		    EndDate = item.EndTime,
		    StartDate = item.StartTime,
		    Id = item.Id,
		    PrePaddingSeconds = item.StartOffset * 60,
		    PostPaddingSeconds = item.EndOffset * 60,
		    RecordAnyChannel = !((item.Filter & RecFilter.ThisChannel) == RecFilter.ThisChannel),
		    RecordAnyTime = !((item.Filter & RecFilter.ThisDayTime) == RecFilter.ThisDayTime),
		    RecordNewOnly = ((item.Filter & RecFilter.NewEpisode) == RecFilter.NewEpisode),
		    ProgramId = item.ProgramId,
		    SeriesId = item.SeriesId,
		    KeepUpTo = item.MaxEpisodes
		};

	    return info;

	}

	private RecRule GetOneRecRule(Stream stream, IJsonSerializer json, ILogger logger)
	{
	    var root = json.DeserializeFromStream<RecRuleRootObject>(stream);
	    UtilsHelper.DebugInformation(logger, string.Format("[MythTV] GetOneRecRule Response: {0}",
							       json.SerializeToString(root)));
	    return root.RecRule;
	}

	public SeriesTimerInfo GetDefaultTimerInfo(Stream stream, IJsonSerializer json, ILogger logger)
        {
	    return RecRuleToSeriesTimerInfo(GetOneRecRule(stream, json, logger));
	}

	public string GetNewSeriesTimerJson(SeriesTimerInfo info, Stream stream, IJsonSerializer json, ILogger logger)
	{

	    RecRule orgRule = GetOneRecRule(stream, json, logger);
	    if (orgRule != null)
	    {
		orgRule.Type = "Record All";

		if (info.RecordAnyChannel)
		    orgRule.Filter &= ~RecFilter.ThisChannel;
		else
		    orgRule.Filter |= RecFilter.ThisChannel;
		if (info.RecordAnyTime)
		    orgRule.Filter &= ~RecFilter.ThisDayTime;
		else
		    orgRule.Filter |= RecFilter.ThisDayTime;
		if (info.RecordNewOnly)
		    orgRule.Filter |= RecFilter.NewEpisode;
		else
		    orgRule.Filter &= ~RecFilter.NewEpisode;

		orgRule.MaxEpisodes = info.KeepUpTo;
		orgRule.StartOffset = info.PrePaddingSeconds / 60;
		orgRule.EndOffset = info.PostPaddingSeconds / 60;
	    
	    }

	    var output = json.SerializeToString(orgRule);
	    logger.Info($"[MythTV RuleResponse: generated new timer json:\n{output}");

	    return output;
	}

	public string GetNewTimerJson(TimerInfo info, Stream stream, IJsonSerializer json, ILogger logger)
	{

	    RecRule rule = GetOneRecRule(stream, json, logger);

	    // check if there is an existing rule that is going to cause grief
	    if (rule.Type != "Not Recording")
		throw new ExistingTimerException(rule.Id);
	    
	    rule.Type = "Single Record";
	    rule.StartOffset = info.PrePaddingSeconds / 60;
	    rule.EndOffset = info.PostPaddingSeconds / 60;

	    var output = json.SerializeToString(rule);
	    logger.Info($"[MythTV RuleResponse: generated new timer json:\n{output}");

	    return output;
	}

	public string GetNewDoNotRecordTimerJson(Stream stream, IJsonSerializer json, ILogger logger)
	{

	    RecRule rule = GetOneRecRule(stream, json, logger);
	    rule.Type = "Do not Record";

	    var output = json.SerializeToString(rule);
	    logger.Info($"[MythTV RuleResponse: generated new timer json:\n{output}");

	    return output;
	}

	[Flags]
	private enum RecFilter
	{
	    NewEpisode = 1,
	    IdentifiableEpisode = 2,
	    FirstShowing = 4,
	    PrimeTime = 8,
	    CommercialFree = 16,
	    HighDefinition = 32,
	    ThisEpisode = 64,
	    ThisSeries = 128,
	    ThisTime = 256,
	    ThisDayTime = 512,
	    ThisChannel = 1024
	}

	private class RecRule
	{
	    public string Id { get; set; }
	    public string ParentId { get; set; }
	    public string Inactive { get; set; }
	    public string Title { get; set; }
	    public string SubTitle { get; set; }
	    public string Description { get; set; }
	    public string Season { get; set; }
	    public string Episode { get; set; }
	    public string Category { get; set; }
	    public DateTime StartTime { get; set; }
	    public DateTime EndTime { get; set; }
	    public string SeriesId { get; set; }
	    public string ProgramId { get; set; }
	    public string Inetref { get; set; }
	    public string ChanId { get; set; }
	    public string CallSign { get; set; }
	    public string FindDay { get; set; }
	    public string FindTime { get; set; }
	    public string Type { get; set; }
	    public string SearchType { get; set; }
	    public string RecPriority { get; set; }
	    public string PreferredInput { get; set; }
	    public int StartOffset { get; set; }
	    public int EndOffset { get; set; }
	    public string DupMethod { get; set; }
	    public string DupIn { get; set; }
	    public RecFilter Filter { get; set; }
	    public string RecProfile { get; set; }
	    public string RecGroup { get; set; }
	    public string StorageGroup { get; set; }
	    public string PlayGroup { get; set; }
	    public bool AutoExpire { get; set; }
	    public int MaxEpisodes { get; set; }
	    public bool MaxNewest { get; set; }
	    public bool AutoCommflag { get; set; }
	    public bool AutoTranscode { get; set; }
	    public bool AutoMetaLookup { get; set; }
	    public bool AutoUserJob1 { get; set; }
	    public bool AutoUserJob2 { get; set; }
	    public bool AutoUserJob3 { get; set; }
	    public bool AutoUserJob4 { get; set; }
	    public string Transcoder { get; set; }
	    public DateTime? NextRecording { get; set; }
	    public DateTime LastRecorded { get; set; }
	    public DateTime LastDeleted { get; set; }
	    public string AverageDelay { get; set; }
	}

	private class RecRuleRootObject
	{
	    public RecRule RecRule { get; set; }
	}

	private class RecRuleList
	{
	    public string StartIndex { get; set; }
	    public string Count { get; set; }
	    public string TotalAvailable { get; set; }
	    public string AsOf { get; set; }
	    public string Version { get; set; }
	    public string ProtoVer { get; set; }
	    public List<RecRule> RecRules { get; set; }
	}

	private class RootObject
	{
	    public RecRuleList RecRuleList { get; set; }
	}
    }
    
    public class DvrResponse
    {
        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

	public List<LiveTvTunerInfo> GetTuners(Stream tunerStream, IJsonSerializer json, ILogger logger)
	{
	    var root = json.DeserializeFromStream<RootObject>(tunerStream);
	    return root.EncoderList.Encoders.Select(i => EncoderToTunerInfo(i)).ToList();
	}

	private LiveTvTunerInfo EncoderToTunerInfo(Encoder tuner)
	{
	    var info = new LiveTvTunerInfo()
                {
                    Id = tuner.Id,
                    Status = (LiveTvTunerStatus)tuner.State,
		    SourceType = tuner.Inputs[0].InputName,
		    Name = $"{tuner.Inputs[0].DisplayName}: {tuner.Id}"
                };

	    switch (tuner.State)
	    {
		case 0:
		    info.Status = LiveTvTunerStatus.Available;
		    break;
		case 7:
		    info.Status = LiveTvTunerStatus.RecordingTv;
		    break;
	    }

	    if(!string.IsNullOrWhiteSpace(tuner.Recording.Title)){
		info.RecordingId = tuner.Recording.ProgramId;
		info.ProgramName = $"{tuner.Recording.Title} : {tuner.Recording.SubTitle}";
	    }

	    return info;
	}

	private class Input
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
	    public List<object> Programs { get; set; }
	}

	private class Artwork
	{
	    public List<object> ArtworkInfos { get; set; }
	}

	private class Cast
	{
	    public List<object> CastMembers { get; set; }
	}

	private class Recording
	{
	    public string StartTime { get; set; }
	    public string EndTime { get; set; }
	    public string Title { get; set; }
	    public string SubTitle { get; set; }
	    public string Category { get; set; }
	    public string CatType { get; set; }
	    public string Repeat { get; set; }
	    public string VideoProps { get; set; }
	    public string AudioProps { get; set; }
	    public string SubProps { get; set; }
	    public string SeriesId { get; set; }
	    public string ProgramId { get; set; }
	    public string Stars { get; set; }
	    public string LastModified { get; set; }
	    public string ProgramFlags { get; set; }
	    public string Airdate { get; set; }
	    public string Description { get; set; }
	    public string Inetref { get; set; }
	    public string Season { get; set; }
	    public string Episode { get; set; }
	    public string TotalEpisodes { get; set; }
	    public string FileSize { get; set; }
	    public string FileName { get; set; }
	    public string HostName { get; set; }
	    public Channel Channel { get; set; }
	    public Artwork Artwork { get; set; }
	    public Cast Cast { get; set; }
	}

	private class Encoder
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

	private class EncoderList
	{
	    public List<Encoder> Encoders { get; set; }
	}

	private class RootObject
	{
	    public EncoderList EncoderList { get; set; }
	}

    }
}
