using babgvant.Emby.MythTv.MythTvCapture;
using babgvant.Emby.MythTv.MythTvChannel;
using babgvant.Emby.MythTv.MythTvContent;
using babgvant.Emby.MythTv.MythTvDvr;
using babgvant.Emby.MythTv.MythTvGuide;
using babgvant.Emby.MythTv.MythTvUtility;
using babgvant.Emby.MythTv.MythTvVideo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv
{
    public class MythServiceHost
    {
        Uri _mythTvPath = null;
        string _userName = string.Empty;
        string _password = string.Empty;

        BasicHttpBinding _binding = null;
        EndpointAddress _mythAddress = null;
        EndpointAddress _guideAddress = null;
        EndpointAddress _videoAddress = null;
        EndpointAddress _dvrAddress = null;
        EndpointAddress _contentAddress = null;
        EndpointAddress _channelAddress = null;
        EndpointAddress _captureAddress = null;

        public DvrClient DvrService { get; private set; }
        public MythClient MythService { get; private set; }
        public GuideClient GuideService { get; private set; }
        public VideoClient VideoService { get; private set; }
        public ContentClient ContentService { get; private set; }
        public ChannelClient ChannelService { get; private set; }
        public CaptureClient CaptureService { get; private set; }

        public MythServiceHost(string hostname) :
            this(hostname, string.Empty, string.Empty) { }

        public MythServiceHost(string mythTvPath, string username, string password)
        {
            _password = password;
            _userName = username;
            _mythTvPath = new Uri(mythTvPath);

            _binding = new BasicHttpBinding();
            _binding.MaxReceivedMessageSize = int.MaxValue;

            if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
            {
                _binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            }

            _mythAddress = new EndpointAddress(string.Format("{0}/Myth", mythTvPath));
            _guideAddress = new EndpointAddress(string.Format("{0}/Guide", mythTvPath));
            _videoAddress = new EndpointAddress(string.Format("{0}/Video", mythTvPath));
            _dvrAddress = new EndpointAddress(string.Format("{0}/Dvr", mythTvPath));
            _contentAddress = new EndpointAddress(string.Format("{0}/Content", mythTvPath));
            _channelAddress = new EndpointAddress(string.Format("{0}/Channel", mythTvPath));
            _captureAddress = new EndpointAddress(string.Format("{0}/Capture", mythTvPath));


            DvrService = new DvrClient(_binding, _dvrAddress);
            MythService = new MythClient(_binding, _mythAddress);
            GuideService = new GuideClient(_binding, _guideAddress);
            VideoService = new VideoClient(_binding, _videoAddress);
            ContentService = new ContentClient(_binding, _contentAddress);
            ChannelService = new ChannelClient(_binding, _channelAddress);
            CaptureService = new CaptureClient(_binding, _captureAddress);

            DvrService.ClientCredentials.UserName.UserName = _userName;
            DvrService.ClientCredentials.UserName.Password = _password;
            MythService.ClientCredentials.UserName.UserName = _userName;
            MythService.ClientCredentials.UserName.Password = _password;
            GuideService.ClientCredentials.UserName.UserName = _userName;
            GuideService.ClientCredentials.UserName.Password = _password;
            VideoService.ClientCredentials.UserName.UserName = _userName;
            VideoService.ClientCredentials.UserName.Password = _password;
            ContentService.ClientCredentials.UserName.UserName = _userName;
            ContentService.ClientCredentials.UserName.Password = _password;
            ChannelService.ClientCredentials.UserName.UserName = _userName;
            ChannelService.ClientCredentials.UserName.Password = _password;
            CaptureService.ClientCredentials.UserName.UserName = _userName;
            CaptureService.ClientCredentials.UserName.Password = _password;
        }
    }
}
