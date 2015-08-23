using babgvant.Emby.MythTv.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public EventHandler ConfigurationChanged;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            RecordingUncs = new List<string>();

            BuildUncList();
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "MythTV"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Provides live tv using MythTV as a back-end.";
            }
        }

        public List<string> RecordingUncs { get; private set; }

        private void BuildUncList()
        {
            RecordingUncs.Clear();

            if (!string.IsNullOrWhiteSpace(this.Configuration.UncPath))
            {
                string[] uncs = this.Configuration.UncPath.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                RecordingUncs.AddRange(uncs);
            }
        }

        public override void SaveConfiguration()
        {
            base.SaveConfiguration();

            BuildUncList();

            EventHandler eh = ConfigurationChanged;
            if (eh != null)
                eh(this, new EventArgs());
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }
    }
}
