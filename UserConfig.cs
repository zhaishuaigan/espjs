using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace espjs
{
    class UserConfig
    {
        public int BaudRate { get; set; }
        public int LastUpdate { get; set; }
        public string Board { get; set; }
        public string Flash { get; set; }
        public Dictionary<string, string> Modules { get; set; }

        /// <summary>
        /// 加载用户配置
        /// </summary>
        /// <returns></returns>
        public static UserConfig Load()
        {
            string workDir = Directory.GetCurrentDirectory();
            return JsonConvert.DeserializeObject<UserConfig>(File.ReadAllText(workDir + @"\espjs.json"));
        }

        /// <summary>
        /// 检测用户配置文件是否存在
        /// </summary>
        /// <returns></returns>
        public static bool Exists()
        {
            string workDir = Directory.GetCurrentDirectory();
            return System.IO.File.Exists(workDir + @"\espjs.json");
        }

        /// <summary>
        /// 保存用户配置
        /// </summary>
        public void Save()
        {
            string workDir = Directory.GetCurrentDirectory();
            string config = FormatJsonString(JsonConvert.SerializeObject(this));
            File.WriteAllText(workDir + @"\espjs.json", config);
        }

        /// <summary>
        /// 格式化json字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FormatJsonString(string str)
        {
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }
    }
}
