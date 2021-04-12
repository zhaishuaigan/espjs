using Newtonsoft.Json;
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

        public static Config Load()
        {
            string execFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string execDir = System.IO.Directory.GetParent(execFile).FullName;
            return JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText(execDir + @"\config.json"));
        }

        public static bool Exists()
        {
            string execFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string execDir = System.IO.Directory.GetParent(execFile).FullName;
            return System.IO.File.Exists(execDir + @"\config.json");
        }
    }
}
