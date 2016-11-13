﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Protocol
{
    class LiveTVPlayback : ProtoMonitor
    {
        private Dictionary<int, ProtoRecorder> m_recorders;
	private int m_idCounter = 0;

        public LiveTVPlayback(string server, int port) : base(server, port)
        {
	    m_recorders = new Dictionary<int, ProtoRecorder>();
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
	    foreach (var recorder in m_recorders)
		await recorder.Value.Close();
            await base.Close();
        }

        public async Task<int> SpawnLiveTV(string chanNum)
        {
            if (!await IsOpen())
                return 0;

            // just bodge it in and get the first free recorder
            var cards = await GetFreeInputs87();

            var recorder = new ProtoRecorder(cards[0].cardId, Server, Port);
            var chain = new Chain();

            if (await recorder.SpawnLiveTV(chain.UID, chanNum))
            {
		m_idCounter++;
		m_recorders.Add(m_idCounter, recorder);
		
                // sleep briefly to make sure file updates
                System.Threading.Thread.Sleep(3000);

                return m_idCounter;
            }

            await recorder.StopLiveTV();
            return 0;
        }

        public async Task<string> GetCurrentRecording(int id)
        {
            var program = await m_recorders[id].GetCurrentRecording75();
            return program.fileName;
        }

        public async Task StopLiveTV(int id)
        {
	    if (m_recorders.ContainsKey(id) && m_recorders[id].IsPlaying)
            {
                await m_recorders[id].StopLiveTV();
		m_recorders.Remove(id);
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
