using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace espjs
{
    class App
    {
        public Uart uart = new Uart();
        public string port = "";
        public string workDir;
        public string execDir;
        public string code = "";
        public string[] args;
        public Config config;
        public UserConfig userConfig;
        public bool hasUserConfig = false;
        public bool runOnce;
        public Dictionary<string, string> fileHashMap = new Dictionary<string, string>();
        public App(bool runOnce = false)
        {
            if (Uart.HasPort())
            {
                port = Uart.GetPort();
            }

            this.runOnce = runOnce;

            if (!runOnce)
            {
                Console.WriteLine("输入help获取帮助信息");
            }

            // 设置工作目录
            this.workDir = Directory.GetCurrentDirectory();
            string execFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            this.execDir = Directory.GetParent(execFile).FullName;

            // 加载基础配置
            this.config = Config.Load();
            uart.sp.BaudRate = this.config.BaudRate;

            // 加载用户配置
            if (UserConfig.Exists())
            {
                this.hasUserConfig = true;
                this.userConfig = UserConfig.Load();

                // 设置串口参数
                this.uart.sp.BaudRate = this.userConfig.BaudRate;
            }
            else
            {
                // 设置串口参数
                this.uart.sp.BaudRate = config.BaudRate;
            }


        }

        /// <summary>
        /// 获取指定位置的参数, 如果没有就返回默认值
        /// </summary>
        /// <param name="num">参数位置</param>
        /// <param name="def">默认值</param>
        /// <returns></returns>
        public string GetParam(int num, string def = "")
        {
            if (args.Length <= num)
            {
                return def;
            }
            return args[num];
        }

        /// <summary>
        /// 获取指定位置参数, 如果没有就让用户输入一个
        /// </summary>
        /// <param name="num">参数位置</param>
        /// <param name="msg">提示用户输入的文本</param>
        /// <returns></returns>
        public string GetParamOrReadLine(int num, string msg = "")
        {
            if (args.Length <= num)
            {
                Console.WriteLine(msg);
                return Console.ReadLine();
            }
            return args[num];
        }

        /// <summary>
        /// 让程序进入下一次循环, 如果指定了程序只运行一次, 则程序会自动退出
        /// </summary>
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

        /// <summary>
        /// 程序入口
        /// </summary>
        /// <param name="args">用户输入的参数</param>
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
                case "init":
                    if (File.Exists(workDir + @"\espjs.json"))
                    {
                        Console.WriteLine("espjs.json 文件已存在, 无法初始化.");
                    }
                    else
                    {
                        File.Copy(execDir + @"\espjs.json", workDir + @"\espjs.json");
                        Console.WriteLine("创建文件 espjs.json 完成.");
                        if (File.Exists(workDir + @"\index.js"))
                        {
                            Console.WriteLine("文件 index.js 已存在.");
                        }
                        else
                        {
                            File.WriteAllText(workDir + @"\index.js", "console.log('hello world');");
                            Console.WriteLine("创建文件 index.js 完成.");
                        }

                        Console.WriteLine("初始化完成.");
                    }

                    break;
                case "version":
                    Console.WriteLine("当前版本: " + this.config.Version);
                    break;
                case "debug":
                    string debug = GetParam(1, "true");
                    if (debug == "true")
                    {
                        uart.showRunCode = true;
                        Console.WriteLine("开启调试");
                    }
                    else
                    {
                        uart.showRunCode = false;
                        Console.WriteLine("关闭调试");
                    }
                    break;
                case "clear":
                    uart.codeHistory.Clear();
                    Console.Clear();
                    break;
                case "atob":
                    string atobStr = GetParamOrReadLine(1, "请输入要解密的字符串: ");
                    Console.WriteLine(uart.Atob(atobStr));
                    break;
                case "btoa":
                    string btoaStr = GetParamOrReadLine(1, "请输入要加密的字符串: ");
                    Console.WriteLine(uart.Btoa(btoaStr));
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
                    ModuleManage();
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
            if (!Uart.HasPort())
            {
                Console.WriteLine("没有可用端口");
                Next();
                return;
            }
            switch (cmd)
            {
                case "restart":
                case "reboot":
                    uart.SendCode(port, "E.reboot();");
                    break;
                case "reset":
                    uart.SendCode(port, "reset(true);");
                    break;
                case "blink":
                    WriteBlinkCode();
                    break;
                case "flash":
                    Flash.Write(port, GetParam(1, ""));
                    break;
                case "rm":
                case "del":
                    name = GetParamOrReadLine(1, "请输入Storage的名称: ");
                    uart.SendCode(port, "require('Storage').erase('" + name + "')");
                    break;
                case "ll":
                case "dir":
                case "storage":
                    Storage();
                    break;
                case "get":
                case "cat":
                    name = GetParamOrReadLine(1, "请输入文件名: ");
                    uart.SendCode(port, "console.log(require('Storage').read('" + name + "'))");
                    break;
                case "exec":
                case "run":
                    uart.SendCode(port, GetParamOrReadLine(1, "请输入代码"));
                    break;
                case "load":
                    LoadFile();
                    break;
                case "upload":
                    Upload();
                    break;
                case "dev":
                    new DevMode(workDir, uart, port).Run();
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

        public void ModuleManage()
        {
            string cmd = GetParam(1, "list");
            string name;
            switch (cmd)
            {
                case "install":
                    Module.Install();
                    break;
                case "ls":
                case "list":
                    Module.Ls();
                    break;
                case "add":
                    name = GetParamOrReadLine(2, "请输入模块名称: ");
                    string url = GetParam(3, "");
                    Module.Add(name, url);
                    break;
                case "remove":
                case "delete":
                    name = GetParamOrReadLine(2, "请输入模块名称: ");
                    Module.Remove(name);
                    break;
            }
        }

        public void Boot()
        {
            if (Uart.HasPort())
            {
            }
            // this.helper.SaveCode();
        }

        public void WriteBlinkCode()
        {
            string code = @"var val = false;setInterval(function(){digitalWrite(NodeMCU.D4,val);val=!val;},1000);";
            uart.SetBootCode(port, code);
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
                    string[] ports = Uart.GetPorts();
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
                    uart.SendCode(port, @"(function(){var list=require('Storage').list();console.log(list.join('\n'));})();");
                    break;
                case "free":
                    uart.SendCode(port, "require('Storage').getFree()");
                    break;
                case "clear":
                    uart.SendCode(port, "require('Storage').eraseAll();E.reboot();");
                    break;
                case "delete":
                case "remove":
                    name = GetParamOrReadLine(2, "请输入Storage的名称: ");
                    uart.SendCode(port, "require('Storage').erase('" + name + "')");
                    break;
                case "get":
                case "read":
                    name = GetParamOrReadLine(2, "请输入Storage的名称: ");
                    uart.SendCode(port, "console.log(require('Storage').read('" + name + "'))");
                    break;
                case "save":
                case "write":
                    uart.SendCode(port, "require('Storage').write('" + GetParamOrReadLine(2, "请输入Storage的名称: ") + "','" + GetParamOrReadLine(3, "请输入Storage的内容: ") + "')");
                    break;
            }

        }

        public void LoadFile()
        {
            string filename = GetParamOrReadLine(1, "请输入文件名: ");
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

        public void Dev()
        {

        }

        public void Upload()
        {
            string path = GetParam(1, workDir);
            string restart = GetParam(2, "true");
            bool uploadAll = true;
            if (path == "changed")
            {
                uploadAll = false;
                path = workDir;
            }
            if (Directory.Exists(path))
            {
                // 文件夹
                string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string name = file.Replace(path + "\\", "").Replace("\\", "/");
                    if (name == "index.js" || name == "main.js")
                    {
                        name = ".bootcde";
                    }

                    string fileTime = File.GetLastWriteTime(file).ToString();
                    bool fileUpdate = true;
                    if (fileHashMap.TryGetValue(name, out string value))
                    {
                        if (value == fileTime)
                        {
                            fileUpdate = false;
                        }
                    }
                    if (uploadAll || fileUpdate)
                    {
                        string code = File.ReadAllText(file);
                        uart.SendFile(port, name, code);
                        Console.WriteLine(name);
                    }
                    else
                    {
                        Console.WriteLine(name + " 未修改");
                    }


                    fileHashMap[name] = fileTime;
                }

                if (restart == "true")
                {
                    uart.SendCode(port, "E.reboot();");
                }

                Console.WriteLine("写入完成");
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

        public void SendFile()
        {
            string filename = GetParamOrReadLine(1, "请输入文件名: ");
            if (!File.Exists(filename))
            {
                filename = workDir + @"\" + filename;
            }


            if (!File.Exists(filename))
            {
                Console.WriteLine("文件不存在: " + filename);
                return;
            }

            string code = System.IO.File.ReadAllText(filename);
            uart.SendCode(port, code);
        }

        public void SendBootCodeFromFile()
        {
            string filename = GetParamOrReadLine(1, "请输入文件名: ");
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

        public void StartShellMode()
        {
            Console.WriteLine("已进入shell模式, 输入exit可以退出shell模式.");
            uart.newLinePrev = "# ";
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
                    uart.SendCode(port, newLine);
                }

            }
            uart.newLinePrev = "> ";
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
                uart.SendCode(port, code);
            }
            code = "";
        }

    }
}
