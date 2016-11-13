using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.LiveTv;
using babgvant.Emby.MythTv.Model;
using babgvant.Emby.MythTv.Helpers;

namespace babgvant.Emby.MythTv.Responses
{
    public class ExistingTimerException : Exception
    {
        public string id { get; private set; }

        public ExistingTimerException(string id)
            : base($"Existing timer {id}")
        {
            this.id = id;
        }
    }

    class RuleResponse
    {

        public IEnumerable<SeriesTimerInfo> GetSeriesTimers(Stream stream, IJsonSerializer json, ILogger logger)
        {

            var root = json.DeserializeFromStream<RootObject>(stream);
            return root.RecRuleList.RecRules
                .Where(rule => rule.Type.Equals("Record All"))
                .Select(i => RecRuleToSeriesTimerInfo(i));

        }

        private SeriesTimerInfo RecRuleToSeriesTimerInfo(RecRule item)
        {
            var info = new SeriesTimerInfo()
            {
                Name = item.Title,
                ChannelId = item.ChanId,
                EndDate = item.EndTime,
                StartDate = item.StartTime,
                Id = item.Id,
                PrePaddingSeconds = item.StartOffset * 60,
                PostPaddingSeconds = item.EndOffset * 60,
                RecordAnyChannel = !((item.Filter & RecFilter.ThisChannel) == RecFilter.ThisChannel),
                RecordAnyTime = !((item.Filter & RecFilter.ThisDayTime) == RecFilter.ThisDayTime),
                RecordNewOnly = ((item.Filter & RecFilter.NewEpisode) == RecFilter.NewEpisode),
                ProgramId = item.ProgramId,
                SeriesId = item.SeriesId,
                KeepUpTo = item.MaxEpisodes
            };

            return info;

        }

        private RecRule GetOneRecRule(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RecRuleRootObject>(stream);
            UtilsHelper.DebugInformation(logger, string.Format("[MythTV] GetOneRecRule Response: {0}",
                                                               json.SerializeToString(root)));
            return root.RecRule;
        }

        public SeriesTimerInfo GetDefaultTimerInfo(Stream stream, IJsonSerializer json, ILogger logger)
        {
            return RecRuleToSeriesTimerInfo(GetOneRecRule(stream, json, logger));
        }

        public string GetNewSeriesTimerJson(SeriesTimerInfo info, Stream stream, IJsonSerializer json, ILogger logger)
        {

            RecRule orgRule = GetOneRecRule(stream, json, logger);
            if (orgRule != null)
            {
                orgRule.Type = "Record All";

                if (info.RecordAnyChannel)
                    orgRule.Filter &= ~RecFilter.ThisChannel;
                else
                    orgRule.Filter |= RecFilter.ThisChannel;
                if (info.RecordAnyTime)
                    orgRule.Filter &= ~RecFilter.ThisDayTime;
                else
                    orgRule.Filter |= RecFilter.ThisDayTime;
                if (info.RecordNewOnly)
                    orgRule.Filter |= RecFilter.NewEpisode;
                else
                    orgRule.Filter &= ~RecFilter.NewEpisode;

                orgRule.MaxEpisodes = info.KeepUpTo;
                orgRule.StartOffset = info.PrePaddingSeconds / 60;
                orgRule.EndOffset = info.PostPaddingSeconds / 60;

            }

            var output = json.SerializeToString(orgRule);
            logger.Info($"[MythTV RuleResponse: generated new timer json:\n{output}");

            return output;
        }

        public string GetNewTimerJson(TimerInfo info, Stream stream, IJsonSerializer json, ILogger logger)
        {

            RecRule rule = GetOneRecRule(stream, json, logger);

            // check if there is an existing rule that is going to cause grief
            if (rule.Type != "Not Recording")
                throw new ExistingTimerException(rule.Id);

            rule.Type = "Single Record";
            rule.StartOffset = info.PrePaddingSeconds / 60;
            rule.EndOffset = info.PostPaddingSeconds / 60;

            var output = json.SerializeToString(rule);
            logger.Info($"[MythTV RuleResponse: generated new timer json:\n{output}");

            return output;
        }

        public string GetNewDoNotRecordTimerJson(Stream stream, IJsonSerializer json, ILogger logger)
        {

            RecRule rule = GetOneRecRule(stream, json, logger);
            rule.Type = "Do not Record";

            var output = json.SerializeToString(rule);
            logger.Info($"[MythTV RuleResponse: generated new timer json:\n{output}");

            return output;
        }

        private class RecRuleRootObject
        {
            public RecRule RecRule { get; set; }
        }

        private class RootObject
        {
            public RecRuleList RecRuleList { get; set; }
        }
    }
}
