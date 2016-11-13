using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Model
{
    public class RecordingDetail
    {
        public string RecordedId { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime StartTs { get; set; }
        public DateTime EndTs { get; set; }
        public string FileSize { get; set; }
        public string FileName { get; set; }
        public string HostName { get; set; }
        public string LastModified { get; set; }
        public string RecordId { get; set; }
        public string RecGroup { get; set; }
        public string PlayGroup { get; set; }
        public string StorageGroup { get; set; }
        public int RecType { get; set; }
        public string DupInType { get; set; }
        public string DupMethod { get; set; }
        public string EncoderId { get; set; }
        public string EncoderName { get; set; }
        public string Profile { get; set; }
    }
}
