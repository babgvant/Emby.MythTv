using Emby.MythTv.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public EventHandler ConfigurationChanged;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            RecordingUncs = new List<string>();
            RecGroupExclude = new List<string>();

            BuildLists();
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
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
        public List<string> RecGroupExclude { get; private set; }

        private void BuildLists()
        {
            RecordingUncs.Clear();

            if (!string.IsNullOrWhiteSpace(this.Configuration.UncPath))
            {
                string[] uncs = this.Configuration.UncPath.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                RecordingUncs.AddRange(uncs);
            }

            RecGroupExclude.Clear();
            if (!string.IsNullOrWhiteSpace(this.Configuration.RecGroupExclude))
            {
                string[] recex = this.Configuration.RecGroupExclude.Split(new string[] { ";","," }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var r in recex)
                    RecGroupExclude.Add(r.Trim());
            }
        }

        public override void SaveConfiguration()
        {
            base.SaveConfiguration();

            BuildLists();

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
