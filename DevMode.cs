using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace espjs
{
    public class DevMode
    {
        public string workDir = "";
        public FileSystemWatcher watcher;
        public Uart uart;
        public string port;
        public Queue files = new Queue();
        public System.Timers.Timer timer = new System.Timers.Timer();
        private readonly UserConfig config;
        private readonly bool hasUserConfig = false;
        public DevMode(string workDir, Uart uart, string port)
        {
            this.workDir = workDir;
            this.uart = uart;
            this.port = port;
            if (UserConfig.Exists())
            {
                hasUserConfig = true;
                config = UserConfig.Load();
            }
        }

        public void Run()
        {
            watcher = new FileSystemWatcher
            {
                Path = workDir,
                IncludeSubdirectories = true,//全局文件监控，包括子目录
                EnableRaisingEvents = true   //启用文件监控
            };
            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            watcher.Changed += new FileSystemEventHandler(OnChanged);


            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += UploadFile;
            timer.Start();

            Console.WriteLine("当前已经进入开发模式, 修改的文件将自动传输到设备.");
            while (true)
            {

                Console.WriteLine("按 C 键退出开发模式, 按 R 重启设备.");
                char devKey = Console.ReadKey(true).KeyChar;
                if (devKey == 'c')
                {
                    Console.WriteLine("已退出开发模式");
                    Stop();
                    break;
                }

                if (devKey == 'r')
                {
                    Console.WriteLine("重启设备");
                    uart.SendCode(port, "E.reboot();");
                }
            }
        }

        private void UploadFile(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            while (files.Count > 0)
            {
                string file = files.Dequeue().ToString();
                string filename = workDir + @"\" + file;
                string name = file.Replace("\\", "/");

                if (InIgnore(name))
                {
                    Console.WriteLine("忽略文件修改: " + name);
                    continue;
                }

                if (name == "index.js" || name == "main.js")
                {
                    name = ".bootcde";
                }

                string code = File.ReadAllText(filename);
                Console.WriteLine("正在写入文件: " + name);
                uart.SendFile(port, name, code);
                Console.WriteLine("文件写入完成: " + name);
            }
            timer.Start();
        }

        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            timer.Stop();
        }


        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            // Console.WriteLine("创建文件: " + e.Name.ToString());
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            // Console.WriteLine("删除文件: " + e.Name.ToString());
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.Name))
            {
                return;
            }
            if (!files.Contains(e.Name))
            {
                files.Enqueue(e.Name);
                Console.WriteLine("文件改动: " + e.Name);
            }


        }

        public bool InIgnore(string path)
        {
            if (!hasUserConfig)
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
