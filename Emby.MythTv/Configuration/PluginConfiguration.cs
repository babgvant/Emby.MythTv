using MediaBrowser.Model.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
	public string Host { get; set; }

        public string WebServiceUrl
        {
            get
            {
                return $"http://{Host}:6544";
            }
        }
        public string UncPath { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool TimeShift { get; set; }
        public Boolean EnableDebugLogging { get; set; }
        public int LiveTvWaits { get; set; }
        public bool LoadChannelIcons { get; set; }
        public string RecGroupExclude { get; set; }

        public PluginConfiguration()
        {
            UserName = string.Empty;
            Password = string.Empty;
            Host = "";
            UncPath = "";
            TimeShift = false;
            EnableDebugLogging = false;
            LiveTvWaits = 10;
            LoadChannelIcons = false;
            RecGroupExclude = "Deleted,LiveTV";
        }
    }
}
