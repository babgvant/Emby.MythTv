using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Model
{
    public class Program
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Category { get; set; }
        public string CatType { get; set; }
        public bool Repeat { get; set; }
        public string VideoProps { get; set; }
        public string AudioProps { get; set; }
        public string SubProps { get; set; }
        public string SeriesId { get; set; }
        public string ProgramId { get; set; }
        public string Stars { get; set; }
        public string FileSize { get; set; }
        public string LastModified { get; set; }
        public string ProgramFlags { get; set; }
        public string FileName { get; set; }
        public string HostName { get; set; }
        public DateTime? Airdate { get; set; }
        public string Description { get; set; }
        public string Inetref { get; set; }
        public string Season { get; set; }
        public string Episode { get; set; }
        public Channel Channel { get; set; }
        public RecordingDetail Recording { get; set; }
        public Artwork Artwork { get; set; }
    }
}
