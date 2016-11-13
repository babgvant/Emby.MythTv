using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Protocol
{
    class ProtoPlayback : ProtoBase
    {

        public ProtoPlayback(string server, int port) : base(server, port)
        {
        }

        public async Task<bool> Open()
        {
            bool ok = false;
            if (!await OpenConnection())
            {
                return false;
            }

            if (ProtoVersion >= 75)
                ok = await Announce75();

            if (ok)
                return true;

            await Close();
            return false;
        }

        public override async Task Close()
        {
            await base.Close();
            m_tainted = m_hang = false;
        }

        private async Task<bool> Announce75()
        {
            var result = await SendCommand("ANN Playback emby 0");
            return result[0] == "OK";
        }

    }
}
