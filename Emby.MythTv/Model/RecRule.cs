using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Model
{
    public class RecRule
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

    [Flags]
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
}
