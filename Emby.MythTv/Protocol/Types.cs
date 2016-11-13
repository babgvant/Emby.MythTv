using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Protocol
{
    public class Program
    {
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public string title { get; set; }
        public string subTitle { get; set; }
        public string description { get; set; }
        public int season { get; set; }
        public int episode { get; set; }
        public string category { get; set; }
        public string catType { get; set; }
        public string hostName { get; set; }
        public string fileName { get; set; }
        public int fileSize { get; set; }
        public bool repeat { get; set; }
        public int programFlags { get; set; }
        public string seriesId { get; set; }
        public string programId { get; set; }
        public string inetref { get; set; }
        public DateTime lastModified { get; set; }
        public string stars { get; set; }
        public DateTime airdate { get; set; }
        public int audioProps { get; set; }
        public int videoProps { get; set; }
        public int subProps { get; set; }
        //Channel channel;
        //Recording recording;
        //vector<Artwork> artwork;

    }

    public class CardInput
    {

        public int inputId { get; set; }
        public int cardId { get; set; }
        public int sourceId { get; set; }
        public int mplexId { get; set; }
        public string inputName { get; set; }
        public int liveTVOrder { get; set; }

        //public CardInput(int inputId, int cardId, int sourceId, int mplexId, string inputName, int liveTVOrder)
        //{
        //    this.inputId = inputId;
        //    this.cardId = cardId;
        //    this.sourceId = sourceId;
        //    this.mplexId = mplexId;
        //    this.inputName = inputName;
        //    this.liveTVOrder = liveTVOrder;
        //}

    }
}
