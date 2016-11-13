using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class VideoSource
    {
        public string Id { get; set; }
        public string SourceName { get; set; }
        public string Grabber { get; set; }
        public string UserId { get; set; }
        public string FreqTable { get; set; }
        public string LineupId { get; set; }
        public bool Password { get; set; }
        public string UseEIT { get; set; }
        public string ConfigPath { get; set; }
        public string NITId { get; set; }
    }
}
