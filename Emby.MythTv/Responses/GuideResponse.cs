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
using babgvant.Emby.MythTv.Model;

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

        private class RootObject
        {
            public ProgramGuide ProgramGuide { get; set; }
        }

    }
}
