using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace espjs
{
    /// <summary>
    /// 上传代码相关操作
    /// </summary>
    public class Upload
    {
        public string workDir = "";
        public Uart uart;
        public string port;
        private readonly UserConfig config;
        public Upload(string workDir, Uart uart, string port)
        {
            this.workDir = workDir;
            this.uart = uart;
            this.port = port;
            if (UserConfig.Exists())
            {
                this.config = UserConfig.Load();
            }
        }

        public void Path(string path)
        {
            path = (path == "" ? this.workDir : path);
            if (Directory.Exists(path))
            {
                // 文件夹
                string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string name = file.Replace(path + "\\", "").Replace("\\", "/");
                    if (InIgnore(name))
                    {
                        continue;
                    }

                    if (name == "index.js" || name == "main.js")
                    {
                        name = ".bootcde";
                    }

                    string code = File.ReadAllText(file);
                    uart.SendFile(port, name, code);
                    Console.WriteLine(name + " 写入完成");
                    // 这里需要暂停一下, 每个文件之间需要间隔一段时间, 否则容易造成单片机死机
                    Thread.Sleep(1000);
                }
                // uart.SendCode(port, "E.reboot();");
                Console.WriteLine("全部文件已写入完成.");
            }
            else if (File.Exists(path))
            {
                // 文件
                string code = File.ReadAllText(path);
                uart.SendFile(port, path, code);
                return;
            }
            else
            {
                Console.WriteLine("文件或文件夹不存在: " + path);
            }
        }


        public void LoadFile(string filename)
        {
            if (!File.Exists(filename))
            {
                filename = workDir + @"\" + filename;
            }


            if (!File.Exists(filename))
            {
                Console.WriteLine("文件不存在: " + filename);
                return;
            }

            string code = File.ReadAllText(filename);
            uart.SendCode(port, code);
        }

        public void SendFile(string filename)
        {
            if (!File.Exists(filename))
            {
                filename = workDir + @"\" + filename;
            }

            if (!File.Exists(filename))
            {
                Console.WriteLine("文件不存在: " + filename);
                return;
            }

            string code = File.ReadAllText(filename);
            uart.SendCode(port, code);
        }

        public void SendBootCodeFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                filename = workDir + @"\" + filename;
            }


            if (!File.Exists(filename))
            {
                Console.WriteLine("文件不存在: " + filename);
                return;
            }

            string code = File.ReadAllText(filename);
            uart.SetBootCode(port, code);
        }

        public void WriteBlinkCode()
        {
            string code = @"var val = false;setInterval(function(){digitalWrite(NodeMCU.D4,val);val=!val;},1000);";
            uart.SetBootCode(port, code);
        }


        public bool InIgnore(string path)
        {
            if (!UserConfig.Exists())
            {
                return false;
            }
            foreach (string value in config.Ignore)
            {
                string rule = value.Replace("*", "");
                if (value.EndsWith("*"))
                {
                    if (path.Length >= rule.Length && rule == path.Substring(0, rule.Length))
                    {
                        return true;
                    }
                }
                else if (rule == path)
                {
                    return true;
                }


            }
            return false;
        }
    }
}
