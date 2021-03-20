using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace espjs
{
    class Config
    {
        public string Version { get; set; }
        public string Modules { get; set; }
        public int BaudRate { get; set; }
        public Dictionary<string, string> Flash { get; set; }
    }
}
