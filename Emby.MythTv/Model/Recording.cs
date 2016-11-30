using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class Recording
    {
        public string RecordedId { get; set; }
        public RecStatus Status { get; set; }
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

    public enum RecStatus
    {
        Pending = -15,
        Failing = -14,
        //OtherRecording = -13, (obsolete)
        //OtherTuning = -12, (obsolete)
        MissedFuture = -11,
        Tuning = -10,
        Failed = -9,
        TunerBusy = -8,
        LowDiskSpace = -7,
        Cancelled = -6,
        Missed = -5,
        Aborted = -4,
        Recorded = -3,
        Recording = -2,
        WillRecord = -1,
        Unknown = 0,
        DontRecord = 1,
        PreviousRecording = 2,
        CurrentRecording = 3,
        EarlierShowing = 4,
        TooManyRecordings = 5,
        NotListed = 6,
        Conflict = 7,
        LaterShowing = 8,
        Repeat = 9,
        Inactive = 10,
        NeverRecord = 11,
        Offline = 12
    }
}
