using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using babgvant.Emby.MythTv.Model;

namespace babgvant.Emby.MythTv.Protocol
{
    class ProtoBase
    {
        protected static readonly string DELIMITER = "[]:[]";

        public enum ERROR_t
        {
            ERROR_NO_ERROR = 0,
            ERROR_SERVER_UNREACHABLE = 1,
            ERROR_SOCKET_ERROR = 2,
            ERROR_UNKNOWN_VERSION = 3,
        }

        public virtual bool IsOpen { get; private set; }
        public uint ProtoVersion { get; private set; }
        public string Server { get; private set; }
        public int Port { get; private set; }
        public bool HasHanging { get; private set; }

        protected bool m_hang = false;
        protected bool m_tainted = false;

        private TcpClient m_socket;

        public ProtoBase(string server, int port)
        {
            ProtoVersion = 88;
            Server = server;
            Port = port;
            IsOpen = false;
        }

        ~ProtoBase()
        {
            Task.WaitAll(Close());
        }

        private string FormatMessage(string message)
        {
            var messageFormat = "{0,-8:G}{1}";
            return string.Format(messageFormat, message.Length, message);
        }

        private async Task<List<string>> sendToServerAsync(string toSend)
        {

            string result;

            try
            {
                var stream = m_socket.GetStream();

                var sendBytes = Encoding.ASCII.GetBytes(toSend);

                await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

                var buffer = new byte[8];
                var bytesRead = await stream.ReadAsync(buffer, 0, 8);

                if (bytesRead == 0)
                {
                    return new[] { "" }.ToList();
                }

                var length = Encoding.ASCII.GetString(buffer, 0, 8);

                var bytesAvailable = int.Parse(length);
                var readBytes = new byte[bytesAvailable];

                var totalBytesRead = 0;
                result = string.Empty;

                while (totalBytesRead < bytesAvailable)
                {
                    bytesRead = await stream.ReadAsync(readBytes, 0, bytesAvailable);
                    totalBytesRead += bytesRead;

                    result += Encoding.ASCII.GetString(readBytes, 0, bytesRead);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return result.Split(new[] { DELIMITER }, StringSplitOptions.None).ToList();
        }

        protected async Task<List<string>> SendCommand(string command)
        {
            return await sendToServerAsync(FormatMessage(command));
        }

        public async Task<bool> OpenConnection()
        {
            m_socket = new TcpClient(Server, Port);
            var result = await SendCommand("MYTH_PROTO_VERSION 88 XmasGift");
            IsOpen = result[0] == "ACCEPT";
            return IsOpen;
        }

        public virtual async Task Close()
        {
            if (m_socket.Connected)
            {
                if (IsOpen && !m_hang)
                {
                    await SendCommand("DONE");
                }
                m_socket.Close();
            }
            IsOpen = false;
        }

        protected Program RcvProgramInfo86(List<string> fields)
        {
            var program = new Program();
            program.Title = fields[0];
            program.SubTitle = fields[1];
            program.Description = fields[2];
            program.Season = int.Parse(fields[3]);
            program.Episode = int.Parse(fields[4]);
            program.Category = fields[7];
            program.FileName = fields[12];
            program.FileSize = long.Parse(fields[13]);
            program.StartTime = UnixTimeStampToDateTime(int.Parse(fields[14]));
            program.EndTime = UnixTimeStampToDateTime(int.Parse(fields[15]));
            return program;
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
