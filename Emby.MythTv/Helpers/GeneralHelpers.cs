using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Helpers
{
    public static class GeneralHelpers
    {
        public static bool ContainsWord(string source, string value, StringComparison comparisonType)
        {
            return source.IndexOf(value, comparisonType) >= 0;
        }
    }

    public static class UtilsHelper
    {
        public static void DebugInformation(ILogger logger, string message)
        {
            var config = Plugin.Instance.Configuration;
            bool enableDebugLogging = config.EnableDebugLogging;

            if (enableDebugLogging)
            {
                logger.Debug(message);
            }
        }
    }
}
