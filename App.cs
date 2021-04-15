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

            if (IsBaseCommand())
            {
                Next();
            }
            else if (!Uart.HasPort())
            {
                Console.WriteLine("没有可用端口");
                Next();
            }
            else
            {
                UartCommand();
                Next();
            }

        }

        /// <summary>
        /// 检测并运行基础命令
        /// </summary>
        /// <returns></returns>
        public bool IsBaseCommand()
        {
            string cmd = GetParam(0, "");
            switch (cmd)
            {
                case "":
                    break;
                case "init":
                    Init.Run(workDir, execDir);
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
                    Console.Write(File.ReadAllText(execDir + @"\help.txt"));
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
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 运行串口相关命令
        /// </summary>
        public void UartCommand()
        {
            string cmd = GetParam(0, "");
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
                    new Upload(workDir, uart, port).WriteBlinkCode();
                    break;
                case "flash":
                    Flash.Write(port, GetParam(1, ""));
                    break;
                case "rm":
                case "del":
                    uart.SendCode(port, "require('Storage').erase('" + GetParamOrReadLine(1, "请输入Storage的名称: ") + "')");
                    break;
                case "ll":
                case "ls":
                case "dir":
                case "free":
                case "storage":
                    Storage.Run(uart, port, args);
                    break;
                case "get":
                case "cat":
                    uart.SendCode(port, "console.log(require('Storage').read('" + GetParamOrReadLine(1, "请输入文件名: ") + "'))");
                    break;
                case "exec":
                case "run":
                    uart.SendCode(port, GetParamOrReadLine(1, "请输入代码"));
                    break;
                case "load":
                    new Upload(workDir, uart, port).LoadFile(GetParamOrReadLine(1, "请输入要加载的文件名: "));
                    break;
                case "upload":
                    new Upload(workDir, uart, port).Path(GetParam(1, ""));
                    break;
                case "dev":
                    new DevMode(workDir, uart, port).Run();
                    break;
                case "boot":
                    new Upload(workDir, uart, port).SendBootCodeFromFile(GetParamOrReadLine(1, "请输入要启动的文件名: "));
                    break;
                case "shell":
                    ShellMode.Run(uart, port);
                    break;
                case "<<<":
                    InputMode.Run(uart, port);
                    break;
                default:
                    Console.WriteLine("命令不存在: " + cmd);
                    break;

            }
        }

        /// <summary>
        /// 模块管理
        /// </summary>
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

        /// <summary>
        /// 端口管理
        /// </summary>
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
                        if (port == this.port)
                        {
                            Console.WriteLine(port + " 当前选择");
                        }
                        else
                        {
                            Console.WriteLine(port);
                        }

                    }
                    break;

                case "current":
                    Console.WriteLine(port);
                    break;

                default:
                    this.port = GetParamOrReadLine(1, "请输入要设置的端口: ");
                    this.port = this.port.ToUpper();
                    break;
            }
        }

    }
}
