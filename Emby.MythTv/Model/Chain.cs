using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    class Chain
    {
        public string UID { get; private set; }

        public Chain()
        {
            UID = $"{System.Net.Dns.GetHostName()}-{DateTime.UtcNow.ToString("o")}";
        }
    }
}
