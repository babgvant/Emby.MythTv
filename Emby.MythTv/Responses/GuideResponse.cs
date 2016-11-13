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
        public DateTime FetchTime { get; set; }
        
        public GuideResponse(Stream stream, IJsonSerializer json)
        {
            root = json.DeserializeFromStream<RootObject>(stream);
            FetchTime = DateTime.Now;
        }
        
        public IEnumerable<ProgramInfo> GetPrograms(string channelId, ILogger logger)
        {
            var listings = root.ProgramGuide.Channels;
            return listings.Where(i => string.Equals(i.ChanId, channelId))
                .SelectMany(i => i.Programs.Select(e => GetProgram(channelId, e)));
        }

        private ProgramInfo GetProgram(string channelId, Program prog)
        {
            var info = new ProgramInfo()
                {

                    /// Id of the program.
                    Id = string.Format("{1}_{0}", ((DateTime)prog.StartTime).Ticks, channelId),

                    /// Gets or sets the channel identifier.
                    ChannelId = channelId,

                    /// Name of the program
                    Name = prog.Title,

                    /// Gets or sets the official rating.
                    // public OfficialRating { get; set; }

                    /// Gets or sets the overview.
                    Overview = prog.Description,

                    /// Gets or sets the short overview.
                    ShortOverview = null,

                    /// The start date of the program, in UTC.
                    StartDate = (DateTime)prog.StartTime,

                    /// The end date of the program, in UTC.
                    EndDate = (DateTime)prog.EndTime,

                    /// Genre of the program.
                    // public List<string> Genres { get; set; }

                    /// Gets or sets the original air date.
                    OriginalAirDate = prog.Airdate,

                    /// Gets or sets a value indicating whether this instance is hd.
                    IsHD = (prog.VideoProps & VideoFlags.VID_HDTV) == VideoFlags.VID_HDTV,

                    Is3D = (prog.VideoProps & VideoFlags.VID_3DTV) == VideoFlags.VID_3DTV,

                    /// Gets or sets the audio.
                    Audio = ConvertAudioFlags(prog.AudioProps), //Hardcode for now (ProgramAudio)item.AudioProps,
                    
                    /// Gets or sets the community rating.
                    CommunityRating = prog.Stars,

                    /// Gets or sets a value indicating whether this instance is repeat.
                    IsRepeat = prog.Repeat,

                    IsSubjectToBlackout = false,

                    /// Gets or sets the episode title.
                    EpisodeTitle = prog.SubTitle,

                    /// Supply the image path if it can be accessed directly from the file system
                    // public string ImagePath { get; set; }

                    /// Supply the image url if it can be downloaded
                    // public string ImageUrl { get; set; }

                    // public string LogoImageUrl { get; set; }

                    /// Gets or sets a value indicating whether this instance has image.
                    // public bool? HasImage { get; set; }

                    /// Gets or sets a value indicating whether this instance is movie.
                    IsMovie = GeneralHelpers.ContainsWord(prog.CatType, "movie", StringComparison.OrdinalIgnoreCase),

                    /// Gets or sets a value indicating whether this instance is sports.
                    IsSports =
                    GeneralHelpers.ContainsWord(prog.Category, "sport",
                                                StringComparison.OrdinalIgnoreCase) ||
                    GeneralHelpers.ContainsWord(prog.Category, "motor sports",
                                                StringComparison.OrdinalIgnoreCase) ||
                    GeneralHelpers.ContainsWord(prog.Category, "football",
                                                StringComparison.OrdinalIgnoreCase) ||
                    GeneralHelpers.ContainsWord(prog.Category, "cricket",
                                                StringComparison.OrdinalIgnoreCase),

                    /// Gets or sets a value indicating whether this instance is series.
                    IsSeries = GeneralHelpers.ContainsWord(prog.CatType, "series", StringComparison.OrdinalIgnoreCase),

                    /// Gets or sets a value indicating whether this instance is live.
                    // public bool IsLive { get; set; }

                    /// Gets or sets a value indicating whether this instance is news.
                    IsNews = GeneralHelpers.ContainsWord(prog.Category, "news",
                                                         StringComparison.OrdinalIgnoreCase),

                    /// Gets or sets a value indicating whether this instance is kids.
                    IsKids = GeneralHelpers.ContainsWord(prog.Category, "animation",
                                                         StringComparison.OrdinalIgnoreCase),

                    // public bool IsEducational { get; set; }

                    /// Gets or sets a value indicating whether this instance is premiere.
                    // public bool IsPremiere { get; set;  }

                    /// Gets or sets the production year.
                    // public int? ProductionYear { get; set; }

                    /// Gets or sets the home page URL.
                    // public string HomePageUrl { get; set; }

                    /// Gets or sets the series identifier.
                    SeriesId = prog.SeriesId,

                    /// Gets or sets the show identifier.
                    ShowId = prog.ProgramId,

                };

            if (prog.Season != null && prog.Season > 0 &&
                prog.Episode != null && prog.Episode > 0)
            {
                info.SeasonNumber = prog.Season;
                info.EpisodeNumber = prog.Episode;
            }
            
            return info;
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

        [Flags]
        private enum VideoFlags
        {
            VID_UNKNOWN       = 0x00,
            VID_HDTV          = 0x01,
            VID_WIDESCREEN    = 0x02,
            VID_AVC           = 0x04,
            VID_720           = 0x08,
            VID_1080          = 0x10,
            VID_DAMAGED       = 0x20,
            VID_3DTV          = 0x40
        }

        [Flags]
        private enum AudioFlags
        {
            AUD_UNKNOWN       = 0x00,
            AUD_STEREO        = 0x01,
            AUD_MONO          = 0x02,
            AUD_SURROUND      = 0x04,
            AUD_DOLBY         = 0x08,
            AUD_HARDHEAR      = 0x10,
            AUD_VISUALIMPAIR  = 0x20,
        }

        private ProgramAudio ConvertAudioFlags(AudioFlags input)
        {
            switch (input)
            {
                case AudioFlags.AUD_STEREO:
                    return ProgramAudio.Stereo;
                case AudioFlags.AUD_DOLBY:
                    return ProgramAudio.Dolby;
            }
            return ProgramAudio.Mono;
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
            public VideoFlags VideoProps { get; set; }
            public AudioFlags AudioProps { get; set; }
            public string SubProps { get; set; }
            public string SeriesId { get; set; }
            public string ProgramId { get; set; }
            public float Stars { get; set; }
            public string FileSize { get; set; }
            public string LastModified { get; set; }
            public string ProgramFlags { get; set; }
            public string FileName { get; set; }
            public string HostName { get; set; }
            public DateTime? Airdate { get; set; }
            public string Description { get; set; }
            public string Inetref { get; set; }
            public int? Season { get; set; }
            public int? Episode { get; set; }
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
