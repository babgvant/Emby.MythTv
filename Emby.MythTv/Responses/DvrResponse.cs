
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
using babgvant.Emby.MythTv.Helpers;
using babgvant.Emby.MythTv.Model;

namespace babgvant.Emby.MythTv.Responses
{    
    public class DvrResponse
    {
        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public List<LiveTvTunerInfo> GetTuners(Stream tunerStream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootObject>(tunerStream);
            return root.EncoderList.Encoders.Select(i => EncoderToTunerInfo(i)).ToList();
        }

        private LiveTvTunerInfo EncoderToTunerInfo(Model.Encoder tuner)
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

        private class RootObject
        {
            public EncoderList EncoderList { get; set; }
        }

    }
}
