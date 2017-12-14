using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.MythTv.Helpers;
using Emby.MythTv.Model;

namespace Emby.MythTv.Responses
{
    public class UtilityResponse
    {
        public static string GetVersion(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = ParseConnectionInfo(stream, json);
            UtilsHelper.DebugInformation(logger, string.Format("[MythTV] GetRecRule Response: {0}", json.SerializeToString(root)));
            return root.ConnectionInfo.Version.Ver;
        }

        private static RootConnectionInfoObject ParseConnectionInfo(Stream stream, IJsonSerializer json)
        {
            using (var reader = new StreamReader(stream, new UTF8Encoding()))
            {
                string resptext = reader.ReadToEnd();
                resptext = Regex.Replace(resptext, "{\"Version\": {\"Version\"", "{\"Version\": {\"Ver\"");
                return json.DeserializeFromString<RootConnectionInfoObject>(resptext);
            }
        }

        private class RootConnectionInfoObject
        {
            public ConnectionInfo ConnectionInfo { get; set; }
        }
    }
}
