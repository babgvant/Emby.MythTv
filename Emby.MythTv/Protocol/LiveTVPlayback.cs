using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Protocol
{
    class LiveTVPlayback : ProtoMonitor
    {
        ProtoRecorder m_recorder;

        public LiveTVPlayback(string server, int port) : base(server, port)
        {
        }

        ~LiveTVPlayback()
        {
            Task.WaitAll(Close());
        }

        public override async Task<bool> Open()
        {
            return await base.Open();
        }

        public override async Task Close()
        {
            await m_recorder.Close();
            await base.Close();
        }

        public async Task<bool> SpawnLiveTV(string chanNum)
        {
            if (!await IsOpen())
                return false;

            await StopLiveTV();

            // just bodge it in and get the first free recorder
            var cards = await GetFreeInputs87();

            m_recorder = new ProtoRecorder(cards[0].cardId, Server, Port);
            var chain = new Chain();

            if (await m_recorder.SpawnLiveTV(chain.UID, chanNum))
            {
                // wait until the file is growing
                //while (new System.IO.FileInfo(m_recorder.GetCurrentRecording75().Result.fileName).Length == 0)
                //    System.Threading.Thread.Sleep(500);

                // sleep briefly to make sure file updates
                System.Threading.Thread.Sleep(3000);
                return true;
            }

            await m_recorder.StopLiveTV();
            return false;
        }

        public async Task<string> GetCurrentRecording()
        {
            var program = await m_recorder.GetCurrentRecording75();
            return program.fileName;
        }

        public async Task StopLiveTV()
        {
            if (m_recorder != null && m_recorder.IsPlaying)
            {
                await m_recorder.StopLiveTV();
                if (m_recorder.IsLiveRecording)
                    m_recorder = null;
            }
        }

        public class Chain
        {
            public string UID { get; private set; }

            public Chain()
            {
                UID = $"{System.Net.Dns.GetHostName()}-{DateTime.UtcNow.ToString("o")}";
            }
        }

    }
}
