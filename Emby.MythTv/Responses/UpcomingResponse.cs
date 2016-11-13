using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.LiveTv;
using babgvant.Emby.MythTv.Model;
using babgvant.Emby.MythTv.Helpers;

namespace babgvant.Emby.MythTv.Responses
{
    public class UpcomingResponse
    {
        public List<TimerInfo> GetUpcomingList(Stream stream, IJsonSerializer json, ILogger logger)
        {

            var root = json.DeserializeFromStream<RootObject>(stream);
            return root.ProgramList.Programs.Select(i => ProgramToTimerInfo(i)).ToList();

        }

        private TimerInfo ProgramToTimerInfo(Program item)
        {

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

            var excluded = Plugin.Instance.RecGroupExclude;
            var root = json.DeserializeFromStream<RootObject>(stream);
            return root.ProgramList.Programs
                .Where(i => !excluded.Contains(i.Recording.RecGroup))
                .Select(i => ProgramToRecordingInfo(i));

        }

        private RecordingInfo ProgramToRecordingInfo(Program item)
        {

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

        private class RootObject
        {
            public ProgramList ProgramList { get; set; }
        }
    }
}
