using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Protocol
{

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
