using babgvant.Emby.MythTv.Helpers;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
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
    public class DvrResponse
    {
        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public static List<TimerInfo> GetTimers(Stream stream, IJsonSerializer json, ILogger logger)
        {
            List<TimerInfo> ret = new List<TimerInfo>();

            var root = ParseRecRules(stream, json);
            foreach (var item in root.RecRuleList.RecRules)
            {
                if (!item.Inactive && string.Compare(item.Type, "Not Recording", true) != 0)
                {
                    TimerInfo val = new TimerInfo()
                    {
                        Name = item.Title,
                        Overview = item.Description,
                        ChannelId = item.ChanId.ToString(),
                        EndDate = (DateTime)item.EndTime,
                        StartDate = (DateTime)item.StartTime,
                        Id = item.Id,
                        PrePaddingSeconds = item.StartOffset * 60,
                        PostPaddingSeconds = item.EndOffset * 60,
                        IsPostPaddingRequired = item.EndOffset != 0,
                        IsPrePaddingRequired = item.StartOffset != 0,
                        ProgramId = item.ProgramId
                    };

                    ret.Add(val);
                }
            }

            return ret;
        }

        public static List<SeriesTimerInfo> GetSeriesTimers(Stream stream, IJsonSerializer json, ILogger logger)
        {
            List<SeriesTimerInfo> ret = new List<SeriesTimerInfo>();

            var root = ParseRecRules(stream, json);
            foreach (var item in root.RecRuleList.RecRules)
            {
                if (!item.Inactive && string.Compare(item.Type, "Single Record", true) != 0 && string.Compare(item.Type, "Not Recording", true) != 0)
                {
                    SeriesTimerInfo val = new SeriesTimerInfo()
                    {
                        Name = item.Title,
                        //Overview = item.Description,
                        ChannelId = item.ChanId.ToString(),
                        EndDate = (DateTime)item.EndTime,
                        StartDate = (DateTime)item.StartTime,
                        Id = item.Id,
                        PrePaddingSeconds = item.StartOffset * 60,
                        PostPaddingSeconds = item.EndOffset * 60,
                        RecordAnyChannel = !((item.Filter & RecFilter.ThisChannel) == RecFilter.ThisChannel),
                        RecordAnyTime = !((item.Filter & RecFilter.ThisDayTime) == RecFilter.ThisDayTime),
                        RecordNewOnly = !((item.Filter & RecFilter.NewEpisode) == RecFilter.NewEpisode),
                        //IsPostPaddingRequired = item.EndOffset != 0,
                        //IsPrePaddingRequired = item.StartOffset != 0,                    
                        ProgramId = item.ProgramId
                    };

                    ret.Add(val);
                }
            }

            return ret;
        }

        public static SeriesTimerInfo GetDefaultTimerInfo(Stream stream, IJsonSerializer json, ILogger logger)
        {
            SeriesTimerInfo val = null;

            var root = ParseRecRule(stream, json);
            UtilsHelper.DebugInformation(logger, string.Format("[MythTV] GetDefaultTimerInfo Response: {0}", json.SerializeToString(root)));
            

            //var root = ParseRecRules(stream, json);

            //foreach (var item in root.RecRuleList.RecRules)
            //{
            //    if (!item.Inactive && item.ChanId == "0")
            //    {
                    val = new SeriesTimerInfo()
                    {
                        PrePaddingSeconds = root.RecRule.StartOffset * 60,
                        PostPaddingSeconds = root.RecRule.EndOffset * 60,
                        RecordAnyChannel = !((root.RecRule.Filter & RecFilter.ThisChannel) == RecFilter.ThisChannel),
                        RecordAnyTime = !((root.RecRule.Filter & RecFilter.ThisDayTime) == RecFilter.ThisDayTime),
                        RecordNewOnly = !((root.RecRule.Filter & RecFilter.NewEpisode) == RecFilter.NewEpisode),
                        //IsPostPaddingRequired = root.RecRule.EndOffset != 0,
                        //IsPrePaddingRequired = root.RecRule.StartOffset != 0,
                    };
            //        break;
            //    }
            //}

            return val;
        }

        public static RecRule GetRecRule(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = ParseRecRule(stream, json);
            UtilsHelper.DebugInformation(logger, string.Format("[MythTV] GetRecRule Response: {0}", json.SerializeToString(root)));
            return root.RecRule;
        }

        public static EncoderList ParseEncoderList(Stream stream, IJsonSerializer json, ILogger logger)
        {
            using (var reader = new StreamReader(stream, new UTF8Encoding()))
            {
                string resptext = reader.ReadToEnd();
                UtilsHelper.DebugInformation(logger, string.Format("[MythTV] ParseEncoderList Response: {0}", resptext));
            
                //resptext = Regex.Replace(resptext, "{\"Version\": {\"Version\"", "{\"Version\": {\"Ver\"");
                var root = json.DeserializeFromString<RootEncoderObject>(resptext);
                return root.EncoderList;
            }           
        }

        private static RecRuleRoot ParseRecRule(Stream stream, IJsonSerializer json)
        {
            return json.DeserializeFromStream<RecRuleRoot>(stream);
        }

        private static RecRuleListRoot ParseRecRules(Stream stream, IJsonSerializer json)
        {
            return json.DeserializeFromStream<RecRuleListRoot>(stream);
        }

        public static RecordId ParseRecordId(Stream stream, IJsonSerializer json)
        {
            return json.DeserializeFromStream<RecordId>(stream);
        }

        public static ProgramList ParseProgramList(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootProgramListObject>(stream);
            return root.ProgramList;
        }

        internal static Program ParseRecorded(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootProgramObject>(stream);
            return root.Program;
        }
    }

    public class RootProgramObject
    {
        public Program Program { get; set; }
    }

    public enum RecFilter
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

    public class RecRule
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public bool Inactive { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Description { get; set; }
        public string Season { get; set; }
        public string Episode { get; set; }
        public string Category { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
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
        public int Transcoder { get; set; }
        public string NextRecording { get; set; }
        public string LastRecorded { get; set; }
        public string LastDeleted { get; set; }
        public string AverageDelay { get; set; }
    }

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

    public class RecRuleListRoot
    {
        public RecRuleList RecRuleList { get; set; }
    }

    public class RecRuleRoot
    {
        public RecRule RecRule { get; set; }
    }

    public class RecordId
    {
        public string @uint { get; set; }
    }

    public class Channel
    {
        public string ChanId { get; set; }
        public string ChanNum { get; set; }
        public string CallSign { get; set; }
        public string IconURL { get; set; }
        public string ChannelName { get; set; }
        public string MplexId { get; set; }
        public string TransportId { get; set; }
        public string ServiceId { get; set; }
        public string NetworkId { get; set; }
        public string ATSCMajorChan { get; set; }
        public string ATSCMinorChan { get; set; }
        public string Format { get; set; }
        public string Modulation { get; set; }
        public string Frequency { get; set; }
        public string FrequencyId { get; set; }
        public string FrequencyTable { get; set; }
        public string FineTune { get; set; }
        public string SIStandard { get; set; }
        public string ChanFilters { get; set; }
        public string SourceId { get; set; }
        public string InputId { get; set; }
        public string CommFree { get; set; }
        public bool UseEIT { get; set; }
        public bool Visible { get; set; }
        public string XMLTVID { get; set; }
        public string DefaultAuth { get; set; }
        public List<Program> Programs { get; set; }
    }

    public class RecordingDetail
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

    public class ArtworkInfo
    {
        public string URL { get; set; }
        public string FileName { get; set; }
        public string StorageGroup { get; set; }
        public string Type { get; set; }
    }

    public class Artwork
    {
        public List<ArtworkInfo> ArtworkInfos { get; set; }
    }

    public class Recording
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
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
        public string FileSize { get; set; }
        public string LastModified { get; set; }
        public string ProgramFlags { get; set; }
        public string FileName { get; set; }
        public string HostName { get; set; }
        public string Airdate { get; set; }
        public string Description { get; set; }
        public string Inetref { get; set; }
        public string Season { get; set; }
        public string Episode { get; set; }
        public Channel Channel { get; set; }
        public RecordingDetail Rec { get; set; }
        public Artwork Artwork { get; set; }
    }

    public class Encoder
    {
        public string Id { get; set; }
        public string HostName { get; set; }
        public string Local { get; set; }
        public string Connected { get; set; }
        public int State { get; set; }
        public string SleepStatus { get; set; }
        public string LowOnFreeSpace { get; set; }
        public Recording Recording { get; set; }
    }

    public class EncoderList
    {
        public List<Encoder> Encoders { get; set; }
    }

    public class RootEncoderObject
    {
        public EncoderList EncoderList { get; set; }
    }

    public class ProgramList
    {
        public string StartIndex { get; set; }
        public string Count { get; set; }
        public string TotalAvailable { get; set; }
        public string AsOf { get; set; }
        public string Version { get; set; }
        public string ProtoVer { get; set; }
        public List<Program> Programs { get; set; }
    }

    public class RootProgramListObject
    {
        public ProgramList ProgramList { get; set; }
    }
}
