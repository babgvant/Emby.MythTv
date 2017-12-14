using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emby.MythTv.Model;

namespace Emby.MythTv.Protocol
{
    class ProtoMonitor : ProtoBase
    {

        public ProtoMonitor(string server, int port) : base(server, port)
        {
        }

        public virtual async Task<bool> Open()
        {
            bool ok = false;

            if (! await OpenConnection())
                return false;

            if (ProtoVersion >= 75)
                ok = await Announce75();

            if (ok)
                return true;

            await Close();
            return false;

        }

        public virtual new async Task<bool> IsOpen()
        {
            if (m_hang)
                return await Open();
            return base.IsOpen;
        }

        public override async Task Close()
        {
            await base.Close();
            m_tainted = m_hang = false;
        }

        public async Task<List<Input>> GetFreeInputs()
        {
            if (ProtoVersion >= 91) return await GetFreeInputs91();
            if (ProtoVersion >= 90) return await GetFreeInputs90();
            if (ProtoVersion >= 89) return await GetFreeInputs89();
            return await GetFreeInputs87();
        }

        public async Task<List<Input>> GetFreeInputs87()
        {
            var input = await SendCommand("GET_FREE_INPUT_INFO 0");
            var output = new List<Input>();

            if (input.Count == 0)
                return output;

            // each card has 11 fields
            if (input.Count % 11 != 0)
                throw new Exception("Expected multiple of 11 fields in GET_FREE_INPUT_INFO response");

            for (int i = 0; i < input.Count; i += 11)
            {
                var curr = input.GetRange(i, 11);
                var card = new Input();

                card.InputName = curr[0];
                card.SourceId = int.Parse(curr[1]);
                card.Id = int.Parse(curr[2]);
                card.CardId = int.Parse(curr[3]);
                card.MplexId = int.Parse(curr[4]);
                card.LiveTVOrder = int.Parse(curr[5]);

                output.Add(card);
            }

            return output;
        }

        public async Task<List<Input>> GetFreeInputs89()
        {
            var input = await SendCommand("GET_FREE_INPUT_INFO 0");
            var output = new List<Input>();

            if (input.Count == 0)
                return output;

            // each card has 12 fields
            if (input.Count % 12 != 0)
                throw new Exception("Expected multiple of 12 fields in GET_FREE_INPUT_INFO response");

            for (int i = 0; i < input.Count; i += 12)
            {
                var curr = input.GetRange(i, 12);
                var card = new Input();

                card.InputName = curr[0];
                card.SourceId = int.Parse(curr[1]);
                card.Id = int.Parse(curr[2]);
                card.CardId = int.Parse(curr[3]);
                card.MplexId = int.Parse(curr[4]);
                card.LiveTVOrder = int.Parse(curr[5]);

                output.Add(card);
            }

            return output;
        }

        public async Task<List<Input>> GetFreeInputs90()
        {
            var input = await SendCommand("GET_FREE_INPUT_INFO 0");
            var output = new List<Input>();

            if (input.Count == 0)
                return output;

            // each card has 12 fields
            if (input.Count % 12 != 0)
                throw new Exception("Expected multiple of 12 fields in GET_FREE_INPUT_INFO response");

            for (int i = 0; i < input.Count; i += 12)
            {
                var curr = input.GetRange(i, 12);
                var card = new Input();

                card.InputName = curr[0];
                card.SourceId = int.Parse(curr[1]);
                card.Id = int.Parse(curr[2]);
                card.CardId = card.Id;
                card.MplexId = int.Parse(curr[4]);
                card.LiveTVOrder = int.Parse(curr[5]);

                output.Add(card);
            }

            return output;
        }

        public async Task<List<Input>> GetFreeInputs91()
        {
            var input = await SendCommand("GET_FREE_INPUT_INFO 0");
            var output = new List<Input>();

            if (input.Count == 0)
                return output;

            // each card has 11 fields
            if (input.Count % 10 != 0)
                throw new Exception("Expected multiple of 10 fields in GET_FREE_INPUT_INFO response");

            for (int i = 0; i < input.Count; i += 10)
            {
                var curr = input.GetRange(i, 10);
                var card = new Input();

                card.InputName = curr[0];
                card.SourceId = int.Parse(curr[1]);
                card.Id = int.Parse(curr[2]);
                card.CardId = card.Id;
                card.MplexId = int.Parse(curr[3]);
                card.LiveTVOrder = int.Parse(curr[4]);

                output.Add(card);
            }

            return output;
        }

        public async Task<bool> Announce75()
        {
            var result = await SendCommand("ANN Monitor emby 0");
            return result[0] == "OK";
        }
    }


}
