using babgvant.Emby.MythTv.Helpers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Responses
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

	private class Version
	{
	    public string Ver { get; set; }
	    public string Branch { get; set; }
	    public string Protocol { get; set; }
	    public string Binary { get; set; }
	    public string Schema { get; set; }
	}

	private class Database
	{
	    public string Host { get; set; }
	    public string Ping { get; set; }
	    public string Port { get; set; }
	    public string UserName { get; set; }
	    public string Password { get; set; }
	    public string Name { get; set; }
	    public string Type { get; set; }
	    public string LocalEnabled { get; set; }
	    public string LocalHostName { get; set; }
	}

	private class WOL
	{
	    public string Enabled { get; set; }
	    public string Reconnect { get; set; }
	    public string Retry { get; set; }
	    public string Command { get; set; }
	}

	private class ConnectionInfo
	{
	    public Version Version { get; set; }
	    public Database Database { get; set; }
	    public WOL WOL { get; set; }
	}

	private class RootConnectionInfoObject
	{
	    public ConnectionInfo ConnectionInfo { get; set; }
	}
    }
}
