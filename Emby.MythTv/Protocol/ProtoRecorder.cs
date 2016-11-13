using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace babgvant.Emby.MythTv.Protocol
{
    class ProtoRecorder : ProtoPlayback
    {
        public int Num { get; set; }
        public bool IsPlaying { get; private set; }
        public bool IsLiveRecording { get; private set; }

        public ProtoRecorder(int num, string server, int port) : base(server, port)
        {
            Num = num;
            IsPlaying = false;
            IsLiveRecording = false;

            Task.WaitAll(Open());
        }

        ~ProtoRecorder()
        {
            if (IsPlaying)
                Task.WaitAll(StopLiveTV());

            Task.WaitAll(Close());
        }

        public async Task<bool> SpawnLiveTV(string chainid, string channum)
        {
            return await SpawnLiveTV75(chainid, channum);
        }

        private async Task<bool> SpawnLiveTV75(string chainid, string channum)
        {
            if (!IsOpen)
                return false;

            var cmd = $"QUERY_RECORDER {Num}{DELIMITER}SPAWN_LIVETV{DELIMITER}{chainid}{DELIMITER}0{DELIMITER}{channum}";

            IsPlaying = true;

            if ((await SendCommand(cmd))[0] != "OK")
                IsPlaying = false;

            return IsPlaying;
        }

        private async Task<bool> StopLiveTV75()
        {
            var cmd = $"QUERY_RECORDER {Num}{DELIMITER}STOP_LIVETV";
            var result = await SendCommand(cmd);
            if (result[0] != "OK")
                return false;

            IsPlaying = false;
            return true;
        }

        public async Task<bool> StopLiveTV()
        {
            return await StopLiveTV75();
        }

        public async Task<Program> GetCurrentRecording75()
        {
            var cmd = $"QUERY_RECORDER {Num}{DELIMITER}GET_CURRENT_RECORDING";
            var result = await SendCommand(cmd);

            return RcvProgramInfo86(result);
        }

    }

}
