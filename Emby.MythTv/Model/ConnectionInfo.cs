using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class ConnectionInfo
    {
        public Version Version { get; set; }
        public Database Database { get; set; }
        public WOL WOL { get; set; }
    }
}
