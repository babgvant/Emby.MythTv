using babgvant.Emby.MythTv.Helpers;
using babgvant.Emby.MythTv.Responses;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv
{
    public class LiveTvService : ILiveTvService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly ILogger _logger;
        //private readonly Dictionary<int, int> _heartBeat = new Dictionary<int, int>();
        private Dictionary<string, Channel> _channelCache = new Dictionary<string, Channel>();
        private readonly AsyncLock _channelsLock = new AsyncLock();
        
        public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
            Plugin.Instance.ConfigurationChanged += OnPluginConfigChange;
        }

        private void OnPluginConfigChange(object sender, EventArgs e)
        {
            EnsureSetup();
        }

        /// <summary>
        /// Ensure that we are connected to the NextPvr server
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private void EnsureSetup()
        {
            var config = Plugin.Instance.Configuration;

            if (string.IsNullOrEmpty(config.WebServiceUrl))
            {
                _logger.Error("[MythTV] Web service url must be configured.");
                throw new InvalidOperationException("MythTV web service url must be configured.");
            }

            //if (string.IsNullOrEmpty(config.UncPath))
            //{
            //    _logger.Error("[MythTV] UncPath must be configured.");
            //    throw new InvalidOperationException("[MythTV] UncPath must be configured.");
            //}
        }

        private HttpRequestOptions PostOptions(CancellationToken cancellationToken, string requestContent, string uriPathQuery, params object[] plist) 
        {
            var options = GetOptions(cancellationToken, uriPathQuery, plist);
            
            if (!string.IsNullOrWhiteSpace(requestContent))
            {
                options.RequestContentType = "application/x-www-form-urlencoded";
                options.RequestContent = requestContent;
            }

            return options;
        }

        private HttpRequestOptions GetOptions(CancellationToken cancellationToken, string uriPathQuery, params object[] plist)
        {
            var options = new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = string.Format("{0}{1}", Plugin.Instance.Configuration.WebServiceUrl, string.Format(uriPathQuery, plist)),
                    AcceptHeader = "application/json"
                };            

            return options;
        }

        private string ConvertJsonRecRuleToPost(string serializedRule)
        {
            string ret = serializedRule
               .Replace("{", string.Empty)
               .Replace("}", string.Empty);
            ret = Regex.Replace(ret, "\"Id\"", "\"RecordId\"");
            ret = Regex.Replace(ret, "\"CallSign\"", "\"Station\"");
            ret = Regex.Replace(ret, "\":\"?", "=");
            ret = Regex.Replace(ret, "\"?,\"", "&");
            ret = Regex.Replace(ret, @"(T\d\d:\d\d:\d\d)\.\d+Z", "$1");
            ret = ret.Replace("\"", string.Empty)
                .Trim();

            return ret;
        }

        private string FormateMythDate(DateTime inDate)
        {
            return inDate.ToString("yyyy-MM-ddThh:mm:ss");
        }

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            List<ChannelInfo> ret = new List<ChannelInfo>();

            _logger.Info("[MythTV] Start GetChannels Async, retrieve all channels");

            await GetCallsign(string.Empty, cancellationToken); //call to build the cache
            using (var releaser = await _channelsLock.LockAsync()) 
            {

		_logger.Info(string.Format("[MythTV] GetChannels: got lock, {0} channels", _channelCache.Count));
                List<string> foundChannels = new List<string>();

                foreach (var channel in _channelCache.Values)
                {

		    _logger.Info(string.Format("[MythTV] Processing {0}", channel.CallSign));
		    
                    if (!foundChannels.Contains(channel.CallSign.ToLower()))
                    {
                        ChannelInfo ci = new ChannelInfo()
                                {
                                    Name = channel.ChannelName,
                                    Number = channel.ChanNum,
                                    Id = channel.ChanId.ToString(_usCulture),
                                    HasImage = false
                                };

                        if (!string.IsNullOrWhiteSpace(channel.IconURL) && Plugin.Instance.Configuration.LoadChannelIcons)
                        {
                            ci.HasImage = true;
                            ci.ImageUrl = string.Format("{0}/Guide/GetChannelIcon?ChanId={1}", Plugin.Instance.Configuration.WebServiceUrl, channel.ChanId);
                        }

                        ret.Add(ci);
                        foundChannels.Add(channel.CallSign.ToLower());
                    }
                }
            }

	    _logger.Info(string.Format("[MythTV] End GetChannels Async, retrieved {0} channels", ret.Count));
	    
            return ret;
        }

        /// <summary>
        /// Gets the Recordings async
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}</returns>
        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            List<RecordingInfo> ret = new List<RecordingInfo>();

            _logger.Info("[MythTV] Start GetRecordings Async, retrieve all 'Pending', 'Inprogress' and 'Completed' recordings ");
            EnsureSetup();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordedList")).ConfigureAwait(false))
            {
                var recordings = DvrResponse.ParseProgramList(stream, _jsonSerializer, _logger);

                foreach (var item in recordings.Programs)
                {
                    if (!Plugin.Instance.RecGroupExclude.Contains(item.Recording.RecGroup))
                    {
                        RecordingInfo val = new RecordingInfo()
                        {
                            Name = item.Title,
                            EpisodeTitle = item.SubTitle,
                            Overview = item.Description,
                            Audio = ProgramAudio.Stereo, //Hardcode for now (ProgramAudio)item.AudioProps,
                            ChannelId = item.Channel.ChanId.ToString(),
                            ProgramId = item.ProgramId,
                            SeriesTimerId = item.Recording.RecordId.ToString(),
                            EndDate = (DateTime)item.EndTime,
                            StartDate = (DateTime)item.StartTime,
                            Url = string.Format("{0}{1}", Plugin.Instance.Configuration.WebServiceUrl, string.Format("/Content/GetFile?StorageGroup={0}&FileName={1}", item.Recording.StorageGroup, item.FileName)),
                            Id = string.Format("StartTime={0}&ChanId={1}", ((DateTime)item.StartTime).Ticks, item.Channel.ChanId),
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
                                    StringComparison.OrdinalIgnoreCase)
                        };

                        if (Plugin.Instance.RecordingUncs.Count > 0)
                        {
                            foreach (string unc in Plugin.Instance.RecordingUncs)
                            {
                                string recPath = Path.Combine(unc, item.FileName);
                                if (File.Exists(recPath))
                                {
                                    val.Path = recPath;
                                    break;
                                }
                            }
                        }
                        val.Genres.AddRange(item.Category.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                        if (item.Artwork.ArtworkInfos.Count() > 0)
                        {
                            val.HasImage = true;
                            val.ImageUrl = string.Format("{0}{1}", Plugin.Instance.Configuration.WebServiceUrl, item.Artwork.ArtworkInfos[0].URL);
                        }
                        else
                            val.HasImage = false;

                        ret.Add(val);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Delete the Recording async from the disk
        /// </summary>
        /// <param name="recordingId">The recordingId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[MythTV] Start Delete Recording Async for recordingId: {0}", recordingId));
            EnsureSetup();

            int chanId = 0;
            long ticks = 0;

            Match m = Regex.Match(recordingId, @"StartTime=(?<start>\d+)&ChanId=(?<chan>\d+)");
            if(m.Success)
            {
                if (int.TryParse(m.Groups["chan"].Value, out chanId) && long.TryParse(m.Groups["start"].Value, out ticks))
                {
                    DateTime start = new DateTime(ticks);
                    _logger.Info(string.Format("[MythTV] Delete Recording Async chan: {0} start: {1}", chanId, start));            
                    //await Host.DvrService.RemoveRecordedAsync(chanId, start);

                    using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/RemoveRecorded?ChanId={0}&StartTime={1}", chanId, FormateMythDate(start))).ConfigureAwait(false))
                    {
                        
                    }    
                }
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "MythTV"; }
        }

        /// <summary>
        /// Cancel pending scheduled Recording 
        /// </summary>
        /// <param name="timerId">The timerId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[MythTV] Start Cancel Recording Async for recordingId: {0}", timerId));
            EnsureSetup();

            using (var stream = await _httpClient.Post(PostOptions(cancellationToken, string.Format("RecordId={0}", timerId), "/Dvr/RemoveRecordSchedule")).ConfigureAwait(false))
            {
                //return new RecordingResponse().GetSeriesTimers(stream, _jsonSerializer, _logger);
            }
        }

        private async Task<string> GetCallsign(string channelId, CancellationToken cancellationToken)
        {
	    _logger.Info("[MythTV] Start GetCallsign");
            using (var releaser = await _channelsLock.LockAsync()) 
            {
                if (_channelCache.Count == 0)
                {
		    _logger.Info("[MythTV] GetCallsign: populating cache");
                    EnsureSetup();

                    using (var sourcesstream = await _httpClient.Get(GetOptions(cancellationToken, "/Channel/GetVideoSourceList")).ConfigureAwait(false))
                    {
                        var sources = ChannelResponse.ParseVideoSourceList(sourcesstream, _jsonSerializer, _logger);
                        foreach (var source in sources.VideoSources)
                        {
                            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Channel/GetChannelInfoList?SourceID={0}&Details=true", source.Id)).ConfigureAwait(false))
                            {
                                var channels = ChannelResponse.ParseChannelInfoList(stream, _jsonSerializer, _logger);
                                foreach (var channel in channels.ChannelInfos)
                                {
				    _logger.Info(string.Format("[MythTV] GetCallsign: processing {0}", channel.CallSign));
                                    if (channel.Visible)
                                    {
                                        _channelCache[channel.ChanId.ToString()] = channel;
                                    }
                                }
                            }
                        }
                    }
		    _logger.Info(string.Format("[MythTV] GetCallsign: populated cache, retrieved {0} callsigns",
					       _channelCache.Count));
                }

                if (_channelCache.ContainsKey(channelId))
                    return _channelCache[channelId].CallSign;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Create a new recording
        /// </summary>
        /// <param name="info">The TimerInfo</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[MythTV] Start CreateTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            EnsureSetup();            

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordSchedule?Template=Default")).ConfigureAwait(false))
            {
                RecRule orgRule = DvrResponse.GetRecRule(stream, _jsonSerializer, _logger);
                if (orgRule != null)
                {
                    orgRule.Title = info.Name;
                    orgRule.ChanId = info.ChannelId;
                    orgRule.CallSign = await GetCallsign(info.ChannelId, cancellationToken);
                    orgRule.EndTime = info.EndDate;
                    orgRule.StartTime = info.StartDate;
                    orgRule.StartOffset = info.PrePaddingSeconds / 60;
                    orgRule.EndOffset = info.PostPaddingSeconds / 60;
                    orgRule.Type = "Single Record";

                    var postContent = ConvertJsonRecRuleToPost(_jsonSerializer.SerializeToString(orgRule));

                    var options = PostOptions(cancellationToken, postContent, "/Dvr/AddRecordSchedule");

                    using (var response = await _httpClient.Post(options).ConfigureAwait(false)) { }
                }
            }
        }

        /// <summary>
        /// Get the pending Recordings.
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetTimer Async, retrieve the 'Pending' recordings");
            EnsureSetup();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordScheduleList")).ConfigureAwait(false))
            {
                return  DvrResponse.GetTimers(stream, _jsonSerializer, _logger);
            }
        }

        /// <summary>
        /// Get the recurrent recordings
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetSeriesTimer Async, retrieve the recurring recordings");
            EnsureSetup();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordScheduleList")).ConfigureAwait(false))
            {
                return  DvrResponse.GetSeriesTimers(stream, _jsonSerializer, _logger);
            }
        }

        /// <summary>
        /// Create a recurrent recording
        /// </summary>
        /// <param name="info">The recurrend program info</param>
        /// <param name="cancellationToken">The CancelationToken</param>
        /// <returns></returns>
        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[MythTV] Start CreateSeriesTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            EnsureSetup();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordSchedule?Template=Default")).ConfigureAwait(false))
            {
                RecRule orgRule = DvrResponse.GetRecRule(stream, _jsonSerializer, _logger);
                if (orgRule != null)
                {
                    orgRule.Title = info.Name;
                    orgRule.ChanId = info.ChannelId;
                    orgRule.CallSign = await GetCallsign(info.ChannelId, cancellationToken);
                    orgRule.EndTime = info.EndDate;
                    orgRule.StartTime = info.StartDate;
                    orgRule.StartOffset = info.PrePaddingSeconds / 60;
                    orgRule.EndOffset = info.PostPaddingSeconds / 60;
                    orgRule.Type = "Record All";
                    //orgRule.FindDay
                    if (info.RecordAnyChannel)
                        orgRule.Filter |= RecFilter.ThisChannel;
                    else
                        orgRule.Filter &= RecFilter.ThisChannel;
                    if (info.RecordAnyTime)
                        orgRule.Filter &= RecFilter.ThisDayTime;
                    else
                        orgRule.Filter |= RecFilter.ThisDayTime;
                    if (info.RecordNewOnly)
                        orgRule.Filter |= RecFilter.NewEpisode;
                    else
                        orgRule.Filter &= RecFilter.NewEpisode;

                    var options = PostOptions(cancellationToken, ConvertJsonRecRuleToPost(_jsonSerializer.SerializeToString(orgRule)), "/Dvr/AddRecordSchedule");

                    using (var response = await _httpClient.Post(options).ConfigureAwait(false)) { }
                }
            }          
        }

        /// <summary>
        /// Update the series Timer
        /// </summary>
        /// <param name="info">The series program info</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[MythTV] Start UpdateSeriesTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            EnsureSetup();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordSchedule?RecordId={0}", info.Id)).ConfigureAwait(false))
            {
                RecRule orgRule = DvrResponse.GetRecRule(stream, _jsonSerializer, _logger);
                if (orgRule != null)
                {
                    orgRule.Title = info.Name;
                    orgRule.ChanId = info.ChannelId;
                    orgRule.CallSign = await GetCallsign(info.ChannelId, cancellationToken);
                    orgRule.EndTime = info.EndDate;
                    orgRule.StartTime = info.StartDate;
                    orgRule.StartOffset = info.PrePaddingSeconds / 60;
                    orgRule.EndOffset = info.PostPaddingSeconds / 60;
                    if (info.RecordAnyChannel)
                        orgRule.Filter |= RecFilter.ThisChannel;
                    else
                        orgRule.Filter &= RecFilter.ThisChannel;
                    if (info.RecordAnyTime)
                        orgRule.Filter &= RecFilter.ThisDayTime;
                    else
                        orgRule.Filter |= RecFilter.ThisDayTime;
                    if (info.RecordNewOnly)
                        orgRule.Filter |= RecFilter.NewEpisode;
                    else
                        orgRule.Filter &= RecFilter.NewEpisode;

                    var options = PostOptions(cancellationToken, ConvertJsonRecRuleToPost(_jsonSerializer.SerializeToString(orgRule)), "/Dvr/UpdateRecordSchedule");

                    using (var response = await _httpClient.Post(options).ConfigureAwait(false)) { }
                }
            }
        }

        /// <summary>
        /// Update a single Timer
        /// </summary>
        /// <param name="info">The program info</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[MythTV] Start UpdateTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            EnsureSetup();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordSchedule?RecordId={0}", info.Id)).ConfigureAwait(false))
            {
                RecRule orgRule = DvrResponse.GetRecRule(stream, _jsonSerializer, _logger);
                if (orgRule != null)
                {
                    orgRule.Title = info.Name;
                    orgRule.ChanId = info.ChannelId;
                    orgRule.CallSign = await GetCallsign(info.ChannelId, cancellationToken);
                    orgRule.EndTime = info.EndDate;
                    orgRule.StartTime = info.StartDate;
                    orgRule.StartOffset = info.PrePaddingSeconds / 60;
                    orgRule.EndOffset = info.PostPaddingSeconds / 60;

                    var options = PostOptions(cancellationToken, ConvertJsonRecRuleToPost(_jsonSerializer.SerializeToString(orgRule)), "/Dvr/UpdateRecordSchedule");

                    using (var response = await _httpClient.Post(options).ConfigureAwait(false)) { }
                }
            }
        }

        /// <summary>
        /// Cancel the Series Timer
        /// </summary>
        /// <param name="timerId">The Timer Id</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[MythTV] Start Cancel SeriesRecording Async for recordingId: {0}", timerId));
            EnsureSetup();

            using (var stream = await _httpClient.Post(PostOptions(cancellationToken, string.Format("RecordId={0}", timerId), "/Dvr/RemoveRecordSchedule")).ConfigureAwait(false))
            {
                //return new RecordingResponse().GetSeriesTimers(stream, _jsonSerializer, _logger);
            }
        }
               
        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MediaSourceInfo> GetChannelStream(string channelOid, string mediaSourceId, CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start ChannelStream");

            throw new NotImplementedException();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordSchedule?Template=Default")).ConfigureAwait(false))
            {
                RecRule orgRule = DvrResponse.GetRecRule(stream, _jsonSerializer, _logger);
                if (orgRule != null)
                {
                    DateTime startTime = DateTime.Now.ToUniversalTime();
                    orgRule.Title = string.Format("Emby LiveTV: {0} ({1}) - {1}", await GetCallsign(channelOid, cancellationToken), channelOid, startTime);
                    orgRule.ChanId = channelOid;
                    orgRule.CallSign = await GetCallsign(channelOid, cancellationToken);
                    orgRule.EndTime = startTime.AddHours(5);
                    orgRule.StartTime = startTime;
                    orgRule.StartOffset = 0;
                    orgRule.EndOffset = 0;
                    orgRule.Type = "Single Record";

                    var postContent = ConvertJsonRecRuleToPost(_jsonSerializer.SerializeToString(orgRule));

                    var options = PostOptions(cancellationToken, postContent, "/Dvr/AddRecordSchedule");

                    using (var response = await _httpClient.Post(options).ConfigureAwait(false))
                    {
                        RecordId recId = DvrResponse.ParseRecordId(response.Content, _jsonSerializer);
                        for (int i = 0; i < Plugin.Instance.Configuration.LiveTvWaits; i++)
                        {
                            await Task.Delay(200).ConfigureAwait(false);
                            try
                            {
                                using (var rpstream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecorded?ChanId={0}&StartTime={1}", channelOid, FormateMythDate(startTime))).ConfigureAwait(false))
                                {

                                var recProg = DvrResponse.ParseRecorded(rpstream, _jsonSerializer,  _logger) ;//Host.DvrService.GetRecorded(int.Parse(channelOid), startTime);
                                if (recProg != null && File.Exists(Path.Combine(Plugin.Instance.Configuration.UncPath, recProg.FileName)))
                                {
                                    return new MediaSourceInfo
                                    {
                                        Id = recId.@uint.ToString(CultureInfo.InvariantCulture),
                                        Path = Path.Combine(Plugin.Instance.Configuration.UncPath, recProg.FileName),
                                        Protocol = MediaProtocol.File,
                                        MediaStreams = new List<MediaStream>
                                        {
                                            new MediaStream
                                            {
                                                Type = MediaStreamType.Video,
                                                // Set the index to -1 because we don't know the exact index of the video stream within the container
                                                Index = -1
                                            },
                                            new MediaStream
                                            {
                                                Type = MediaStreamType.Audio,
                                                // Set the index to -1 because we don't know the exact index of the audio stream within the container
                                                Index = -1
                                            }
                                        }
                                    };
                                    break;
                                }

                                }
                            }
                            catch
                            {
                                _logger.Info("GetChannelStream wait {0} for {1}", i, channelOid);
                            }
                        }
                    }
                }

                throw new ResourceNotFoundException(string.Format("Could not stream channel {0}", channelOid));
            }            
        }

        public async Task<MediaSourceInfo> GetRecordingStream(string recordingId, string mediaSourceId, CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetRecordingStream");
            var recordings = await GetRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var recording = recordings.First(i => string.Equals(i.Id, recordingId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(recording.Url))
            {
                _logger.Info("[MythTV] RecordingUrl: {0}", recording.Url);
                return new MediaSourceInfo
                {
                    Path = recording.Url,
                    Protocol = MediaProtocol.Http,
                    MediaStreams = new List<MediaStream>
                        {
                            new MediaStream
                            {
                                Type = MediaStreamType.Video,
                                // Set the index to -1 because we don't know the exact index of the video stream within the container
                                Index = -1,

                                // Set to true if unknown to enable deinterlacing
                                IsInterlaced = true
                            },
                            new MediaStream
                            {
                                Type = MediaStreamType.Audio,
                                // Set the index to -1 because we don't know the exact index of the audio stream within the container
                                Index = -1,

                                // Set to true if unknown to enable deinterlacing
                                IsInterlaced = true
                            }
                        }
                };
            }

            if (!string.IsNullOrEmpty(recording.Path) && File.Exists(recording.Path))
            {
                _logger.Info("[MythTV] RecordingPath: {0}", recording.Path);
                return new MediaSourceInfo
                {
                    Path = recording.Path,
                    Protocol = MediaProtocol.File,
                    MediaStreams = new List<MediaStream>
                        {
                            new MediaStream
                            {
                                Type = MediaStreamType.Video,
                                // Set the index to -1 because we don't know the exact index of the video stream within the container
                                Index = -1,

                                // Set to true if unknown to enable deinterlacing
                                IsInterlaced = true
                            },
                            new MediaStream
                            {
                                Type = MediaStreamType.Audio,
                                // Set the index to -1 because we don't know the exact index of the audio stream within the container
                                Index = -1,

                                // Set to true if unknown to enable deinterlacing
                                IsInterlaced = true
                            }
                        }
                };
            }

            _logger.Error("[MythTV] No stream exists for recording {0}", recording);
            throw new ResourceNotFoundException(string.Format("No stream exists for recording {0}", recording));
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Closing " + id);

            throw new NotImplementedException();

            await CancelTimerAsync(id, cancellationToken);
        }

        public async Task CopyFilesAsync(StreamReader source, StreamWriter destination)
        {
            _logger.Info("[MythTV] Start CopyFiles Async");
            char[] buffer = new char[0x1000];
            int numRead;
            while ((numRead = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await destination.WriteAsync(buffer, 0, numRead);
            }
        }

        public async Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            _logger.Info("[MythTV] Start GetNewTimerDefault Async");
            EnsureSetup();
            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordSchedule?Template=Default")).ConfigureAwait(false))
            {
                return DvrResponse.GetDefaultTimerInfo(stream, _jsonSerializer, _logger);
            }               
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetPrograms Async, retrieve programs for: {0}", channelId);
            EnsureSetup();
            var options = GetOptions(cancellationToken, "/Guide/GetProgramGuide?StartTime={0}&EndTime={1}&StartChanId={2}&NumChannels=1&Details=1", FormateMythDate(startDateUtc), FormateMythDate(endDateUtc), channelId);

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new GuideResponse().GetPrograms(stream, _jsonSerializer, channelId, _logger).ToList();
            }
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public event EventHandler DataSourceChanged;

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            EnsureSetup();
            
            bool upgradeAvailable = false;
            string serverVersion = string.Empty;

            var conInfoTask = _httpClient.Get(GetOptions(cancellationToken, "/Myth/GetConnectionInfo")).ConfigureAwait(false);

            var tunersTask = _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetEncoderList")).ConfigureAwait(false);
            var encodersTask = _httpClient.Get(GetOptions(cancellationToken, "/Capture/GetCaptureCardList")).ConfigureAwait(false); 

            EncoderList tuners = null;
            CaptureCardList encoders = null;

            using (var stream = await tunersTask)
            {
                tuners = DvrResponse.ParseEncoderList(stream, _jsonSerializer, _logger);
            }

            using (var stream = await encodersTask)
            {
                encoders = CaptureResponse.ParseCaptureCardList(stream, _jsonSerializer, _logger);
            }

            using (var stream = await conInfoTask)
            {
                var conInfo = UtilityResponse.GetConnectionInfo(stream, _jsonSerializer, _logger);
                serverVersion = conInfo.Version.Ver;
            }
            
            //Tuner information
            List<LiveTvTunerInfo> tvTunerInfos = new List<LiveTvTunerInfo>();
            foreach(var tuner in tuners.Encoders)
            {
                LiveTvTunerInfo info = new LiveTvTunerInfo()
                {
                    Id = tuner.Id.ToString(),
                    Status = (LiveTvTunerStatus)tuner.State
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
                    info.ProgramName = string.Format("{0} : {1}", tuner.Recording.Title, tuner.Recording.SubTitle);
                }

                foreach(var enc in encoders.CaptureCards)
                {
                    if(enc.CardId == tuner.Id)
                    {
                        info.Name = string.Format("{0} {1}", enc.CardType, enc.VideoDevice);
                        info.SourceType = enc.CardType;
                        break;
                    }
                }
                
                tvTunerInfos.Add(info);
            }

            return new LiveTvServiceStatusInfo
            {
                HasUpdateAvailable = upgradeAvailable,
                Version = serverVersion,
                Tuners = tvTunerInfos
            };
        }

        public string HomePageUrl
        {
            get { return "http://www.mythtv.org/"; }
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ChannelInfo
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ProgramInfo
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to RecordingInfo
            throw new NotImplementedException();
        }
    }

}
