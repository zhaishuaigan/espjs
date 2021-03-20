using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace espjs
{
    class App
    {
        public Helper helper = new Helper();
        public string port = "";
        public string workDir;
        public string execDir;
        public string code = "";
        public string[] args;
        public Config config;
        public bool runOnce;
        public App(bool runOnce = false)
        {
            if (Helper.HasPort())
            {
                port = Helper.GetPort();
            }

            this.runOnce = runOnce;

            if (!runOnce)
            {
                Console.WriteLine("输入help获取帮助信息");
            }

            // 设置工作目录
            this.workDir = System.IO.Directory.GetCurrentDirectory();
            string execFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            this.execDir = System.IO.Directory.GetParent(execFile).FullName;

            // 加载配置
            string json = System.IO.File.ReadAllText(execDir + @"\config.json");
            this.config = JsonConvert.DeserializeObject<Config>(json);

            // 设置串口参数
            this.helper.sp.BaudRate = config.BaudRate;
        }

        public string GetParam(int num, string def = "")
        {
            if (args.Length <= num)
            {
                return def;
            }
            return args[num];
        }

        public string GetParamOrReadLine(int num, string msg = "")
        {
            if (args.Length <= num)
            {
                Console.WriteLine(msg);
                return Console.ReadLine();
            }
            return args[num];
        }

        public void Next()
        {
            if (this.runOnce)
            {
                Environment.Exit(0);
            }
            Console.Write("> ");
            args = Console.ReadLine().Trim().Split(' ');
            Run(args);
        }

        public void Run(string[] args)
        {
            this.args = args;
            string name;
            string cmd = GetParam(0, "");
            bool keep = false;
            switch (cmd)
            {
                case "":
                    break;
                case "version":
                    Console.WriteLine("当前版本: " + this.config.Version);
                    break;
                case "debug":
                    string debug = GetParam(1, "true");
                    if (debug == "true")
                    {
                        helper.showRunCode = true;
                        Console.WriteLine("开启调试");
                    }
                    else
                    {
                        helper.showRunCode = false;
                        Console.WriteLine("关闭调试");
                    }
                    break;
                case "clear":
                    helper.codeHistory.Clear();
                    Console.Clear();
                    break;
                case "atob":
                    string atobStr = GetParamOrReadLine(1, "请输入要解密的字符串: ");
                    Console.WriteLine(helper.Atob(atobStr));
                    break;
                case "btoa":
                    string btoaStr = GetParamOrReadLine(1, "请输入要加密的字符串: ");
                    Console.WriteLine(helper.Btoa(btoaStr));
                    break;
                case "help":
                    Help();
                    break;
                case "exit":
                case "quit":
                    Environment.Exit(0);
                    break;
                case "port":
                    Port();
                    break;
                case "module":
                case "modules":
                    Module();
                    break;
                default:
                    keep = true;
                    break;
            }

            if (!keep)
            {
                Next();
                return;
            }
            if (!Helper.HasPort())
            {
                Console.WriteLine("没有可用端口");
                Next();
                return;
            }
            switch (cmd)
            {
                case "restart":
                case "reboot":
                    helper.SendCode(port, "E.reboot();");
                    break;
                case "reset":
                    helper.SendCode(port, "reset(true);");
                    break;
                case "blink":
                    WriteBlinkCode();
                    break;
                case "flash":
                    Flash();
                    break;
                case "rm":
                case "del":
                    name = GetParamOrReadLine(1, "请输入Storage的名称: ");
                    helper.SendCode(port, "require('Storage').erase('" + name + "')");
                    break;
                case "ll":
                case "dir":
                case "storage":
                    Storage();
                    break;
                case "get":
                case "cat":
                    name = GetParamOrReadLine(1, "请输入文件名: ");
                    helper.SendCode(port, "console.log(require('Storage').read('" + name + "'))");
                    break;
                case "exec":
                case "run":
                    helper.SendCode(port, GetParamOrReadLine(1, "请输入代码"));
                    break;
                case "load":
                    LoadFile();
                    break;
                case "upload":
                    Upload();
                    break;
                case "boot":
                    SendBootCodeFromFile();
                    break;
                case "shell":
                    StartShellMode();
                    break;
                case "<<<":
                    StartInputMode();
                    break;
                default:
                    Console.WriteLine("命令不存在: " + cmd);
                    break;

            }
            Next();

        }

        public void Help()
        {
            Console.WriteLine(" 命令 \t\t 帮助内容");
            Console.WriteLine(" help \t\t 显示帮助内容");
            Console.WriteLine(" exit|quit \t 退出程序");
            Console.WriteLine(" port \t\t 列出所有设备");
            Console.WriteLine(" port com3 \t 选择com3设备");
            Console.WriteLine(" flash [board] \t 写入固件, 目前支持 esp01,esp01s,esp8288, 如果有其他板子, 可以配置config.json");
            Console.WriteLine(" blink \t\t 写入闪烁灯程序");
            Console.WriteLine(" boot [filename] \t 写入启动程序, 如: boot index.js 将把当前目录中的index.js文件写入到设备的 .bootcde");
            Console.WriteLine(" load [filename] \t 执行单个代码文件, 如: load blink.js 将在设备中执行当前目录下的blink.js文件");
            Console.WriteLine(" upload [filename|dir] \t 将目录或文件写入设备, 如: upload 将把当前目录中的所有文件直接写入设备, upload blink.js 将把 blink.js写入到设备的 blink.js");
            Console.WriteLine(" ll|ls|dir \t 列出设备中的Storage");
            Console.WriteLine(" storage [option] \t Storage相关操作");
            Console.WriteLine("     list \t\t 列出设备中的Storage");
            Console.WriteLine("     clear \t\t 清除设备中所有的Storage");
            Console.WriteLine("     write [name] [content] \t 写入Storage");
            Console.WriteLine("     read [name] \t\t 读取Storage");
            Console.WriteLine("     delete [name] \t\t 删除Storage");
            Console.WriteLine(" exec|run [code] \t 在设备中运行单行js, 代码不可包含空格");
            Console.WriteLine(" shell \t\t 进入设备执行js, 输入exit|quit退出shell模式");
            Console.WriteLine(" <<< \t\t 进入粘贴代码模式, 在新行输入再次输入 <<< 退出粘贴模式, 并提示是否运行代码");



        }

        public void Module()
        {
            string cmd = GetParam(1, "list");
            string dir = workDir + @"\modules\";
            string name;
            string file;
            switch (cmd)
            {
                case "ls":
                case "list":
                    if (!System.IO.Directory.Exists(dir))
                    {
                        Console.WriteLine("当前没有安装模块");
                        return;
                    }
                    string[] files = System.IO.Directory.GetFiles(dir);
                    foreach (string value in files)
                    {
                        Console.WriteLine(value.Replace(dir, "").Replace(".min.js", ""));
                    }
                    break;
                case "add":
                    name = GetParamOrReadLine(2, "请输入模块名称: ");
                    try
                    {
                        string code = GetWebContent(config.Modules.Replace("[name]", name));
                        if (!System.IO.Directory.Exists(dir))
                        {
                            System.IO.Directory.CreateDirectory(dir);
                        }
                        System.IO.File.WriteAllText(dir + name + ".min.js", code);
                        Console.WriteLine("模块" + name + "下载完成");
                    }
                    catch (System.Net.WebException)
                    {
                        Console.WriteLine("模块下载失败, 请检测模块是否存在");
                        return;
                    }
                    break;
                case "remove":
                case "delete":
                    if (!System.IO.Directory.Exists(dir))
                    {
                        Console.WriteLine("当前没有安装模块");
                        return;
                    }
                    name = GetParamOrReadLine(2, "请输入模块名称: ");
                    file = dir + name + ".min.js";
                    if (System.IO.File.Exists(file))
                    {
                        System.IO.File.Delete(file);
                    }

                    Console.WriteLine("模块删除成功");
                    break;
            }
        }

        public string GetWebContent(string url)
        {
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            //声明一个HttpWebRequest请求
            request.Timeout = 30000;
            //设置连接超时时间
            request.Headers.Set("Pragma", "no-cache");
            System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream streamReceive = response.GetResponseStream();
            Encoding encoding = Encoding.GetEncoding("UTF-8");
            System.IO.StreamReader streamReader = new StreamReader(streamReceive, encoding);
            return streamReader.ReadToEnd();
        }

        public void Boot()
        {
            if (Helper.HasPort())
            {
            }
            // this.helper.SaveCode();
        }

        public void WriteBlinkCode()
        {
            string code = @"var val = false;setInterval(function(){digitalWrite(NodeMCU.D4,val);val=!val;},1000);";
            helper.SetBootCode(port, code);
        }

        public void Flash()
        {


            string name = GetParamOrReadLine(1, "请输入开发板类型: ");
            if (config.Flash.TryGetValue(name, out string value))
            {
                string cmd = "esptool.exe " + value.Replace("[port]", this.port);
                string bat = execDir + @"\resources\flash.bat";
                System.IO.File.WriteAllText(bat, cmd);
                // 这里需要关闭端口, 否则会导致esptool下载固件失败
                helper.ClosePort();
                LaunchBat(bat);
            }
            else
            {
                Console.WriteLine("错误: 没有在config.json中找到对用的flash配置");
            }
        }
        public void LaunchBat(string batName, string argument = "", string workingDirectory = "")
        {

            if (workingDirectory == "")
            {
                workingDirectory = System.IO.Directory.GetParent(batName).FullName;
            }
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                FileName = batName,
                Arguments = argument,
                WorkingDirectory = workingDirectory
            };
            // startInfo.WindowStyle = ProcessWindowStyle.Maximized;
            Process exe = Process.Start(startInfo);
            exe.WaitForExit();
        }

        public void Port()
        {
            string cmd = GetParam(1, "list");
            switch (cmd)
            {
                case "list":
                    string[] ports = Helper.GetPorts();
                    if (ports.Length == 0)
                    {
                        Console.WriteLine("没有可用端口");
                    }
                    foreach (string port in ports)
                    {
                        Console.WriteLine(port);
                    }
                    break;

                case "current":
                    Console.WriteLine(port);
                    break;

                default:
                    port = GetParamOrReadLine(1, "请输入要设置的端口: ");
                    break;
            }
        }

        public void Storage()
        {
            string name;
            switch (GetParam(1, "list"))
            {
                case "ls":
                case "list":
                    helper.SendCode(port, @"(function(){var list=require('Storage').list();console.log(list.join('\n'));})();");
                    break;
                case "free":
                    helper.SendCode(port, "require('Storage').getFree()");
                    break;
                case "clear":
                    helper.SendCode(port, "require('Storage').eraseAll();E.reboot();");
                    break;
                case "delete":
                case "remove":
                    name = GetParamOrReadLine(2, "请输入Storage的名称: ");
                    helper.SendCode(port, "require('Storage').erase('" + name + "')");
                    break;
                case "get":
                case "read":
                    name = GetParamOrReadLine(2, "请输入Storage的名称: ");
                    helper.SendCode(port, "console.log(require('Storage').read('" + name + "'))");
                    break;
                case "save":
                case "write":
                    helper.SendCode(port, "require('Storage').write('" + GetParamOrReadLine(2, "请输入Storage的名称: ") + "','" + GetParamOrReadLine(3, "请输入Storage的内容: ") + "')");
                    break;
            }

        }

        public void LoadFile()
        {
            string filename = GetParamOrReadLine(1, "请输入文件名: ");
            if (!System.IO.File.Exists(filename))
            {
                filename = workDir + @"\" + filename;
            }


            if (!System.IO.File.Exists(filename))
            {
                Console.WriteLine("文件不存在: " + filename);
                return;
            }

            string code = System.IO.File.ReadAllText(filename);
            helper.SendCode(port, code);
        }

        public void Upload()
        {
            string path = GetParam(1, workDir);
            if (System.IO.Directory.Exists(path))
            {
                // 文件夹
                string[] files = System.IO.Directory.GetFiles(path, "*.js", System.IO.SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string name = file.Replace(path + "\\", "").Replace("\\", "/");
                    if (name == "index.js" || name == "main.js")
                    {
                        name = ".bootcde";
                    }
                    string code = System.IO.File.ReadAllText(file);
                    helper.SendFile(port, name, code);
                    Console.WriteLine(name);
                }
                helper.SendCode(port, "E.reboot();");
                Console.WriteLine("写入完成");
            }
            else if (System.IO.File.Exists(path))
            {
                // 文件
                string code = System.IO.File.ReadAllText(path);
                helper.SendFile(port, path, code);
                return;
            }
            else
            {
                Console.WriteLine("文件或文件夹不存在: " + path);
            }

        }

        public void SendFile()
        {
            string filename = GetParamOrReadLine(1, "请输入文件名: ");
            if (!System.IO.File.Exists(filename))
            {
                filename = workDir + @"\" + filename;
            }


            if (!System.IO.File.Exists(filename))
            {
                Console.WriteLine("文件不存在: " + filename);
                return;
            }

            string code = System.IO.File.ReadAllText(filename);
            helper.SendCode(port, code);
        }

        public void SendBootCodeFromFile()
        {
            string filename = GetParamOrReadLine(1, "请输入文件名: ");
            if (!System.IO.File.Exists(filename))
            {
                filename = workDir + @"\" + filename;
            }


            if (!System.IO.File.Exists(filename))
            {
                Console.WriteLine("文件不存在: " + filename);
                return;
            }

            string code = System.IO.File.ReadAllText(filename);
            helper.SetBootCode(port, code);
        }

        public void StartShellMode()
        {
            Console.WriteLine("已进入shell模式, 输入exit可以退出shell模式.");
            helper.newLinePrev = "# ";
            while (true)
            {
                Console.Write("# ");
                string newLine = Console.ReadLine();
                if (newLine == "exit" || newLine == "quit")
                {
                    Console.WriteLine("退出shell模式.");
                    break;
                }

                if (newLine != "")
                {
                    helper.SendCode(port, newLine);
                }

            }
            helper.newLinePrev = "> ";
        }

        public void StartInputMode()
        {
            code = "";
            while (true)
            {
                Console.Write("# ");
                string newLine = Console.ReadLine();
                if (newLine == "<<<")
                {
                    break;
                }
                code += "\n" + newLine;
            }
            Console.Write("是否运行代码(y/n): ");
            if (Console.ReadLine() != "n")
            {
                helper.SendCode(port, code);
            }
            code = "";
        }

    }
}
