using System;
using System.IO;
using System.Text;

namespace espjs
{
    /// <summary>
    /// 模块管理相关操作 增删改查
    /// </summary>
    public class Module
    {
        public static string workDir = Directory.GetCurrentDirectory();
        public static string execDir = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
        public static string moduleDir = workDir + @"\modules\";

        /// <summary>
        /// 根据用户配置安装全部模块
        /// </summary>
        public static void Install()
        {
            if (Directory.Exists(moduleDir) == false)
            {
                Directory.CreateDirectory(moduleDir);
            }

            if (UserConfig.Exists() == false)
            {
                Console.WriteLine("用户配置文件 espjs.json 不存在.");
                return;
            }

            UserConfig config = UserConfig.Load();

            foreach (var item in config.Modules)
            {
                Add(item.Key, item.Value);
            }
            Console.WriteLine("模块安装成功.");

        }

        /// <summary>
        /// 添加模块
        /// </summary>
        /// <param name="name">模块名</param>
        /// <param name="url">模块地址</param>
        /// <returns></returns>
        public static bool Add(string name, string url)
        {
            try
            {
                if (url == "")
                {
                    url = Config.Load().Modules.Replace("[name]", name);
                }
                string code = GetWebContent(url);
                if (!Directory.Exists(moduleDir))
                {
                    Directory.CreateDirectory(moduleDir);
                }
                File.WriteAllText(moduleDir + name + ".min.js", code);
                Console.WriteLine("模块" + name + "下载完成");
                if (UserConfig.Exists())
                {
                    UserConfig config = UserConfig.Load();
                    if (config.Modules.ContainsKey(name))
                    {
                        config.Modules[name] = url;
                    }
                    else
                    {
                        config.Modules.Add(name, url);
                    }

                    config.Save();
                }
                return true;
            }
            catch (System.Net.WebException)
            {
                Console.WriteLine("模块下载失败, 请检测模块是否存在");
                return false;
            }
        }

        /// <summary>
        /// 删除模块
        /// </summary>
        /// <param name="name">模块名</param>
        public static void Remove(string name)
        {
            if (!Directory.Exists(moduleDir))
            {
                Console.WriteLine("当前没有安装模块");
                return;
            }
            string file = moduleDir + name + ".min.js";
            if (File.Exists(file))
            {
                if (UserConfig.Exists())
                {
                    UserConfig config = UserConfig.Load();
                    if (config.Modules.ContainsKey(name))
                    {
                        config.Modules.Remove(name);
                    }
                    config.Save();
                }
                File.Delete(file);
            }

            Console.WriteLine("模块删除成功");
        }

        /// <summary>
        /// 显示已经安装的模块列表
        /// </summary>
        public static void Ls()
        {
            if (!Directory.Exists(moduleDir))
            {
                Console.WriteLine("当前没有安装模块");
                return;
            }
            string[] files = Directory.GetFiles(moduleDir);
            foreach (string value in files)
            {
                Console.WriteLine(value.Replace(moduleDir, "").Replace(".min.js", ""));
            }
        }

        /// <summary>
        /// 获取网页内容
        /// </summary>
        /// <param name="url">网页地址</param>
        /// <returns></returns>
        public static string GetWebContent(string url)
        {
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            //声明一个HttpWebRequest请求
            request.Timeout = 30000;
            //设置连接超时时间
            request.Headers.Set("Pragma", "no-cache");
            System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            Stream streamReceive = response.GetResponseStream();
            Encoding encoding = Encoding.GetEncoding("UTF-8");
            StreamReader streamReader = new StreamReader(streamReceive, encoding);
            return streamReader.ReadToEnd();
        }


    }
}
