using System;
using System.IO;
using System.Text;

namespace espjs
{
    /// <summary>
    /// 模块管理相关操作 增删改查
    /// </summary>
    public class Script
    {
        public static string workDir = Directory.GetCurrentDirectory();
        public static string execDir = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;


        public static bool TryGetValue(string name, out string value)
        {
            value = "";
            if (UserConfig.Exists() == false)
            {
                Console.WriteLine("用户配置文件 espjs.json 不存在.");
                return false;
            }

            UserConfig config = UserConfig.Load();
            if (config.Scripts.TryGetValue(name, out string v))
            {
                value = v;
                return true;
            }
            else
            {
                Console.WriteLine("未在 espjs.json 中定义脚本[" + name + "].");
                return false;
            }
        }

    }
}
