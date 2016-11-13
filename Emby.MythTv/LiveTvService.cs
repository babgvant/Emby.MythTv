using babgvant.Emby.MythTv.Helpers;
using babgvant.Emby.MythTv.Responses;
using babgvant.Emby.MythTv.Protocol;
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
        private LiveTVPlayback _liveTV;

        // cache the listings data
        private readonly AsyncLock _guideLock = new AsyncLock();
        private GuideResponse _guide;

        // cache the channelId -> chanNum map for liveTV
        private readonly AsyncLock _channelLock = new AsyncLock();
        private Dictionary<string, string> channelNums;

        
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

            if (string.IsNullOrEmpty(config.Host))
            {
                _logger.Error("[MythTV] Host must be configured.");
                throw new InvalidOperationException("MythTV host must be configured.");
            }

            //if (string.IsNullOrEmpty(config.UncPath))
            //{
            //    _logger.Error("[MythTV] UncPath must be configured.");
            //    throw new InvalidOperationException("[MythTV] UncPath must be configured.");
            //}

            if (_liveTV == null)
            {
                _logger.Info("[MythTV] Initiating MythProtocol connection");
                _liveTV = new LiveTVPlayback(config.Host, 6543);
                _liveTV.Open();
            }
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

        private string FormatMythDate(DateTime inDate)
        {
            return inDate.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetChannels Async, retrieve all channels");

            var sources = await GetVideoSourceList(cancellationToken);
            var channels = new List<ChannelInfo>();
            foreach (var sourceId in sources) {
                
                var options = GetOptions(cancellationToken,
                                         "/Channel/GetChannelInfoList?SourceID={0}&Details=true",
                                         sourceId);
                    
                using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
                {
                    channels.AddRange(ChannelResponse.GetChannels(stream, _jsonSerializer, _logger,
                                                                  Plugin.Instance.Configuration.LoadChannelIcons));
                }
            }

            using (var releaser = await _channelLock.LockAsync()) {
                channelNums = channels.ToDictionary(i => i.Id, i => i.Number);
            }
            
            return channels;
        }

        /// <summary>
        /// Gets the Recordings async
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}</returns>
        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {

            _logger.Info("[MythTV] Start GetRecordings Async, retrieve all 'Pending', 'Inprogress' and 'Completed' recordings ");
            EnsureSetup();

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetRecordedList")).ConfigureAwait(false))
            {
                return new DvrResponse().GetRecordings(stream, _jsonSerializer, _logger);
            }

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

            var options = PostOptions(cancellationToken,
                                      $"RecordedId={recordingId}",
                                      "/Dvr/DeleteRecording");
            await _httpClient.Post(options).ConfigureAwait(false);

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

            _logger.Info($"[MythTV] Start Cancel Recording Async for recordingId: {timerId}");

            // A timer coming from a series timer will have a ficticious id
            // of the form xxx_yyyy
            // In this case we have to create a new 'do not record' rule for the program
            if (timerId.Contains('_'))
            {
                var ChannelId = timerId.Split('_')[0];
                var StartDate = new DateTime(Convert.ToInt64(timerId.Split('_')[1]));
                await CreateDoNotRecordTimerAsync(ChannelId, StartDate, cancellationToken);
                return;
            }

            // We are cancelling a legitimate single timer
            EnsureSetup();

            var options = PostOptions(cancellationToken, $"RecordId={timerId}", "/Dvr/RemoveRecordSchedule");
            await _httpClient.Post(options).ConfigureAwait(false);
        
        }

        private async Task<IEnumerable<string>> GetVideoSourceList(CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetVideoSourceList");

            var options = GetOptions(cancellationToken,
                                     "/Channel/GetVideoSourceList");

            using (var sourcesstream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return ChannelResponse.GetVideoSourceList(sourcesstream, _jsonSerializer, _logger);
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

            var timerJson = _jsonSerializer.SerializeToString(info);
            _logger.Info($"[MythTV] Start CreateTimer Async for TimerInfo\n{timerJson}");

            EnsureSetup();

            var options = GetRuleStreamOptions(info.ChannelId, info.StartDate, info.ProgramId, cancellationToken);
            using (var stream = await _httpClient.Get(options))
            {
                try
                {
                    var json = new DvrResponse().GetNewTimerJson(info, stream, _jsonSerializer, _logger);
                    var post = PostOptions(cancellationToken,
                                           ConvertJsonRecRuleToPost(json),
                                           "/Dvr/AddRecordSchedule");
                    await _httpClient.Post(post).ConfigureAwait(false);
                }
                catch (ExistingTimerException existing)
                {
                    _logger.Info($"[MythTV] found existing rule {existing.id}");
                    await CancelTimerAsync(existing.id, cancellationToken);
                }
            }          

        }

        private async Task CreateDoNotRecordTimerAsync(string ChannelId, DateTime StartDate,
                                                       CancellationToken cancellationToken)
        {

            _logger.Info($"[MythTV] Start CreateDoNotRecordTimer Async for Channel {ChannelId} at {StartDate}");

            EnsureSetup();

            var options = GetRuleStreamOptions(ChannelId, StartDate, null, cancellationToken, true);
            using (var stream = await _httpClient.Get(options))
            {
                var json = new DvrResponse().GetNewDoNotRecordTimerJson(stream, _jsonSerializer, _logger);
                var post = PostOptions(cancellationToken, ConvertJsonRecRuleToPost(json), "/Dvr/AddRecordSchedule");
                await _httpClient.Post(post).ConfigureAwait(false);
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

            using (var stream = await _httpClient.Get(GetOptions(cancellationToken, "/Dvr/GetUpcomingList?ShowAll=false")).ConfigureAwait(false))
            {
                return  new DvrResponse().GetUpcomingList(stream, _jsonSerializer, _logger);
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
                return new DvrResponse().GetSeriesTimers(stream, _jsonSerializer, _logger);
            }
        }

        private HttpRequestOptions GetRuleStreamOptions(string ChanId, DateTime StartDate, string ProgramId,
                                                        CancellationToken cancellationToken,
                                                        bool MakeOverride = false)
        {
            //split the program id back into channel + starttime if ChannelId not defined
            if (ChanId.Equals("0"))
                ChanId = ProgramId.Split('_')[0];

            var StartTime = FormatMythDate(StartDate);

            var url = $"/Dvr/GetRecordSchedule?ChanId={ChanId}&StartTime={StartTime}";
            if (MakeOverride)
                url = url + "&MakeOverride=true";

            //now get myth to generate the standard recording template for the program
            return GetOptions(cancellationToken, url);
        }

        /// <summary>
        /// Create a recurrent recording
        /// </summary>
        /// <param name="info">The recurrend program info</param>
        /// <param name="cancellationToken">The CancelationToken</param>
        /// <returns></returns>
        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {

            var seriesTimerJson = _jsonSerializer.SerializeToString(info);
            _logger.Info($"[MythTV] Start CreateSeriesTimer Async for SeriesTimerInfo\n{seriesTimerJson}");

            EnsureSetup();

            var options = GetRuleStreamOptions(info.ChannelId, info.StartDate, info.ProgramId, cancellationToken);
            using (var stream = await _httpClient.Get(options))
            {
                var json = new DvrResponse().GetNewSeriesTimerJson(info, stream, _jsonSerializer, _logger);
                var post = PostOptions(cancellationToken, ConvertJsonRecRuleToPost(json), "/Dvr/AddRecordSchedule");
                await _httpClient.Post(post).ConfigureAwait(false);
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
            var seriesTimerJson = _jsonSerializer.SerializeToString(info);
            _logger.Info($"[MythTV] Start UpdateSeriesTimer Async for SeriesTimerInfo\n{seriesTimerJson}");

            EnsureSetup();

            var options = GetRuleStreamOptions(info.ChannelId, info.StartDate, info.ProgramId, cancellationToken);
            using (var stream = await _httpClient.Get(options))
            {
                var json = new DvrResponse().GetNewSeriesTimerJson(info, stream, _jsonSerializer, _logger);
                var post = PostOptions(cancellationToken, ConvertJsonRecRuleToPost(json), "/Dvr/UpdateRecordSchedule");
                await _httpClient.Post(post).ConfigureAwait(false);
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
            var timerJson = _jsonSerializer.SerializeToString(info);
            _logger.Info($"[MythTV] Start UpdateTimer Async for TimerInfo\n{timerJson}");

            EnsureSetup();

            var options = GetRuleStreamOptions(info.ChannelId, info.StartDate, info.ProgramId, cancellationToken);
            using (var stream = await _httpClient.Get(options))
            {
                var json = new DvrResponse().GetNewTimerJson(info, stream, _jsonSerializer, _logger);
                var post = PostOptions(cancellationToken, ConvertJsonRecRuleToPost(json), "/Dvr/UpdateRecordSchedule");
                await _httpClient.Post(post).ConfigureAwait(false);
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

            var options = PostOptions(cancellationToken,
                                      $"RecordId={timerId}",
                                      "/Dvr/RemoveRecordSchedule");
            await _httpClient.Post(options).ConfigureAwait(false);

        }
               
        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MediaSourceInfo> GetChannelStream(string channelId, string mediaSourceId, CancellationToken cancellationToken)
        {
            _logger.Info($"[MythTV] Start ChannelStream for {channelId}");

            // await GetChannels if channelNums isn't populated
            if (channelNums == null)
                await GetChannelsAsync(cancellationToken);
            
            var id = await _liveTV.SpawnLiveTV(channelNums[channelId]);
            if (id == 0)
                return new MediaSourceInfo();
            
            var filepath = await _liveTV.GetCurrentRecording(id);

            _logger.Info($"[MythTV] ChannelStream at {filepath}");

            var output = new MediaSourceInfo
            {
                Id = id.ToString(),
                Path = filepath,
                Protocol = MediaProtocol.File,
                ReadAtNativeFramerate = true,
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
                },
                SupportsProbing = false,
                IsInfiniteStream = true
            };

            return output;
        }

        public async Task<MediaSourceInfo> GetRecordingStream(string recordingId, string mediaSourceId, CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetRecordingStream");
            var recordings = await GetRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var recording = recordings.First(i => string.Equals(i.Id, recordingId, StringComparison.OrdinalIgnoreCase));

            var output = new MediaSourceInfo
                {
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

            if (!string.IsNullOrEmpty(recording.Url))
            {
                _logger.Info("[MythTV] RecordingUrl: {0}", recording.Url);
                output.Path = recording.Url;
                output.Protocol = MediaProtocol.Http;

                return output;
            }

            if (!string.IsNullOrEmpty(recording.Path) && File.Exists(recording.Path))
            {
                _logger.Info("[MythTV] RecordingPath: {0}", recording.Path);
                output.Path = recording.Path;
                output.Protocol = MediaProtocol.File;

                return output;
            }

            _logger.Error("[MythTV] No stream exists for recording {0}", recording);
            throw new ResourceNotFoundException(string.Format("No stream exists for recording {0}", recording));
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info($"[MythTV] Closing {id}");
            _liveTV.StopLiveTV(int.Parse(id));
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
                return new DvrResponse().GetDefaultTimerInfo(stream, _jsonSerializer, _logger);
            }               
        }

        private async Task CacheGuideResponse(DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            using (var releaser = await _guideLock.LockAsync()) {
            
                if (_guide != null && (DateTime.Now - _guide.FetchTime).Hours < 1)
                    return;

                _logger.Info("[MythTV] Start CacheGuideResponse");

                EnsureSetup();
            
                var options = GetOptions(cancellationToken,
                                         "/Guide/GetProgramGuide?StartTime={0}&EndTime={1}&Details=1",
                                         FormatMythDate(startDateUtc),
                                         FormatMythDate(endDateUtc));
                // This can be slow so default 20 sec timeout can be too short
                options.TimeoutMs = 60000;

                using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
                {
                    _guide = new GuideResponse(stream, _jsonSerializer);
                }
            }

            _logger.Info("[MythTV] End CacheGuideResponse");
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            _logger.Info("[MythTV] Start GetPrograms Async, retrieve programs for: {0}", channelId);

            await CacheGuideResponse(startDateUtc, endDateUtc, cancellationToken);
            
            using (var releaser = await _guideLock.LockAsync())
            {
                return _guide.GetPrograms(channelId, _logger).ToList();
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

            List<LiveTvTunerInfo> tvTunerInfos;
            using (var tunerStream = await tunersTask)
            {
                tvTunerInfos = new DvrResponse().GetTuners(tunerStream, _jsonSerializer, _logger);
            }

            using (var stream = await conInfoTask)
            {
                serverVersion = UtilityResponse.GetVersion(stream, _jsonSerializer, _logger);
            }
            
            //Tuner information

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
