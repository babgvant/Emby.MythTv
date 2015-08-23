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
        private string _webServiceUrl;

        public string WebServiceUrl
        {
            get
            {
                return _webServiceUrl;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && value.EndsWith("/"))
                    _webServiceUrl = value.Remove(value.Length - 1);
                else
                    _webServiceUrl = value;
            }
        }
        public string UncPath { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool TimeShift { get; set; }
        public Boolean EnableDebugLogging { get; set; }
        public int LiveTvWaits { get; set; }
        public bool LoadChannelIcons { get; set; }

        public PluginConfiguration()
        {
            UserName = string.Empty;
            Password = string.Empty;
            WebServiceUrl = "";
            UncPath = "";
            TimeShift = false;
            EnableDebugLogging = false;
            LiveTvWaits = 10;
            LoadChannelIcons = false;
        }
    }
}
