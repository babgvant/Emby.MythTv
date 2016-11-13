using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Model
{
    public class Program
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Category { get; set; }
        public string CatType { get; set; }
        public bool Repeat { get; set; }
        public VideoFlags VideoProps { get; set; }
        public AudioFlags AudioProps { get; set; }
        public string SubProps { get; set; }
        public string SeriesId { get; set; }
        public string ProgramId { get; set; }
        public float Stars { get; set; }
        public string FileSize { get; set; }
        public string LastModified { get; set; }
        public string ProgramFlags { get; set; }
        public string FileName { get; set; }
        public string HostName { get; set; }
        public DateTime? Airdate { get; set; }
        public string Description { get; set; }
        public string Inetref { get; set; }
        public int? Season { get; set; }
        public int? Episode { get; set; }
        public Channel Channel { get; set; }
        public RecordingDetail Recording { get; set; }
        public Artwork Artwork { get; set; }
    }

    [Flags]
    public enum VideoFlags
    {
        VID_UNKNOWN = 0x00,
        VID_HDTV = 0x01,
        VID_WIDESCREEN = 0x02,
        VID_AVC = 0x04,
        VID_720 = 0x08,
        VID_1080 = 0x10,
        VID_DAMAGED = 0x20,
        VID_3DTV = 0x40
    }

    [Flags]
    public enum AudioFlags
    {
        AUD_UNKNOWN = 0x00,
        AUD_STEREO = 0x01,
        AUD_MONO = 0x02,
        AUD_SURROUND = 0x04,
        AUD_DOLBY = 0x08,
        AUD_HARDHEAR = 0x10,
        AUD_VISUALIMPAIR = 0x20,
    }
}
