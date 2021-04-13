using System;
using System.Diagnostics;
using System.IO;

namespace espjs
{
    /// <summary>
    /// 写入Espruino固件专用类
    /// </summary>
    public class Flash
    {
        public static void Write(string port, string board)
        {
            Config systemConfig = Config.Load();
            UserConfig config = new UserConfig();
            if (UserConfig.Exists())
            {
                config = UserConfig.Load();
            }
            if (board != "")
            {

                if (systemConfig.Flash.ContainsKey(board))
                {
                    if (UserConfig.Exists())
                    {
                        config.Board = board;
                        config.Save();
                    }
                    RunEspTool(port, systemConfig.Flash[board]);
                }
                else
                {
                    Console.WriteLine("开发板 [" + board + "] 暂不支持烧写固件");
                }

            }
            else if (UserConfig.Exists())
            {
                if (config.Flash != "")
                {
                    RunEspTool(port, config.Flash);
                }
                else if (config.Board != "")
                {
                    if (systemConfig.Flash.ContainsKey(config.Board))
                    {
                        RunEspTool(port, systemConfig.Flash[config.Board]);
                    }
                    else
                    {
                        Console.WriteLine("开发板 [" + config.Board + "] 暂不支持烧写固件");
                    }
                }
                else
                {
                    Console.WriteLine("请输入开发板名称");
                }
            }
            else
            {
                Console.WriteLine("请输入开发板名称");
            }
        }

        public static void RunEspTool(string port, string param)
        {
            string cmd = "esptool.exe " + param.Replace("[port]", port);
            string execFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string execDir = Directory.GetParent(execFile).FullName;
            string bat = execDir + @"\resources\flash.bat";
            File.WriteAllText(bat, cmd);
            // 这里需要关闭端口, 否则会导致esptool下载固件失败
            LaunchBat(bat);
        }

        public static void LaunchBat(string batName, string argument = "", string workingDirectory = "")
        {

            if (workingDirectory == "")
            {
                workingDirectory = Directory.GetParent(batName).FullName;
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

    }
}
