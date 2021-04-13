using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;

namespace espjs
{
    /// <summary>
    /// 串口相关操作
    /// </summary>
    public class Uart
    {
        public SerialPort sp;
        public string newLinePrev = "> ";
        public ArrayList codeHistory = new ArrayList();
        public bool showRunCode = false;
        public Uart()
        {
            this.sp = new SerialPort();
            sp.BaudRate = 115200;
            sp.DataBits = 8;
            this.sp.DataReceived += DataReceived;
        }

        public static string[] GetPorts()
        {
            return SerialPort.GetPortNames();
        }

        public static string GetPort()
        {
            string[] ports = GetPorts();
            return ports.Length >= 1 ? ports[ports.Length - 1] : "";
        }

        public static bool HasPort()
        {
            string[] ports = GetPorts();
            return ports.Length >= 1;
        }

        public static string GetRootPath()
        {
            return Directory.GetCurrentDirectory();
        }

        public static string GetExecDir()
        {
            return GetRootPath();
        }

        public string[] GetEspruinoBinList()
        {
            string binPath = GetRootPath() + "/bin/espruino/";
            FileInfo[] files = new DirectoryInfo(binPath).GetFiles();
            string[] result = new String[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                result[i] = files[i].Name;
            }
            return result;
        }

        public void DownloadEspruinoToEsp01s(string port)
        {
            var baud = "115200";
            var bin = "./espruino/espruino_2v08_esp8266_combined_512.bin";
            var flashSize = "512KB";
            var argument = "--port " + port + " --baud " + baud + " write_flash --flash_size " + flashSize + " 0x0000 " + bin;
            DownloadEspruino(argument);
        }

        public void DownloadEspruinoToEsp8266(string port)
        {
            var baud = "115200";
            var bin = "./espruino/espruino_2v08_esp8266_4mb_combined_4096.bin";
            var flashSize = "4MB";
            var argument = "--port " + port + " --baud " + baud + " write_flash --flash_size " + flashSize + " 0x0000 " + bin;
            DownloadEspruino(argument);
        }
        public void DownloadEspruino(string argument)
        {

            LaunchBat(Environment.CurrentDirectory + "/bin/espruino.bat", argument);
        }

        public void LaunchBat(string batName, string argument)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                FileName = batName,
                Arguments = argument
            };
            // startInfo.WindowStyle = ProcessWindowStyle.Maximized;
            Process.Start(startInfo);
        }

        public void SendCode(string port, string code)
        {
            try
            {
                OpenPort(port);
                this.codeHistory.Add(code);

                if (this.codeHistory.Count > 100)
                {
                    this.codeHistory.RemoveAt(0);
                }
                Thread.Sleep(100);
                sp.WriteLine(code);
                //sp.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("代码发送失败, 可能端口被占用了");
            }

        }

        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string str = sp.ReadLine();
                str = new Regex("^>").Replace(str, "").TrimEnd();
                switch (str)
                {
                    case "":
                    case ">":
                    case " ":
                    case "=undefined":
                    case "=true":
                    case "=function () { [native code] }":

                        break;
                    default:

                        if (this.codeHistory.IndexOf(str) != -1)
                        {
                            // 这里判断是否输出运行代码
                            if (this.showRunCode)
                            {
                                Console.WriteLine(" 执行: " + str);
                                Console.Write(newLinePrev);
                            }
                        }
                        else
                        {
                            Console.WriteLine(" >>> " + str);
                            Console.Write(newLinePrev);
                        }
                        break;
                }

            }
            catch (Exception)
            {
                Console.WriteLine(">>> 输出解析异常");
                Console.Write(newLinePrev);
            }
            // MessageBox.Show(str);
            // sp.Close();
        }

        public string BuildCode(string code)
        {
            code = new Regex("\r").Replace(code, "");
            code = new Regex("\n").Replace(code, @"\n");
            code = code.Replace("'", @"\'");
            return code;
        }

        public string Btoa(string value)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
        }

        public string Atob(string value)
        {
            string decodedString = "";
            if (value.Length % 4 > 0)
            {
                value += new string('=', 4 - value.Length % 4);
            }
            try
            {
                byte[] data = Convert.FromBase64String(value);
                decodedString = System.Text.Encoding.UTF8.GetString(data);
            }
            catch (System.FormatException)
            {
                Console.WriteLine("字符串解密失败");
            }
            return decodedString;
        }

        public static int GetZhLen(string str)
        {
            int count = 0;
            Regex regex = new Regex(@"^[\u4E00-\u9FA5]{1,}$");
            for (int i = 0; i < str.Length; i++)
            {
                if (regex.IsMatch(str[i].ToString()))
                {
                    count++;
                }
            }

            return str.Length + count * 2;
        }

        /// <summary>
        /// 生成写入文件的代码
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <param name="code">要写入的代码</param>
        /// <returns>生成的JS代码</returns>
        public ArrayList BuildFileWriteCode(string filename, string code)
        {

            code = new Regex("\r").Replace(code, "");
            code = code.Trim();
            int codeLen = GetZhLen(code);
            filename = filename.Trim();
            ArrayList result = new ArrayList
            {
                "f = require('Storage');",
            };

            int len = 100;
            if (code.Length <= len)
            {
                result.Add("f.write('" + filename + "',atob('" + Btoa(code) + "'));");
            }
            else
            {

                int current = 0;
                int zhCurrent = 0;
                string buff = code.Substring(current, len);
                int buffLen = GetZhLen(buff);
                result.Add("f.write('" + filename + "',atob('" + Btoa(buff) + "'),0," + codeLen + ");");
                current += len;
                zhCurrent += buffLen;
                while (true)
                {
                    if (code.Length >= current + len)
                    {
                        buff = code.Substring(current, len);
                        buffLen = GetZhLen(buff);
                        result.Add("f.write('" + filename + "',atob('" + Btoa(buff) + "')," + zhCurrent + ");");
                        current += len;
                        zhCurrent += buffLen;
                    }
                    else
                    {
                        buff = code.Substring(current);
                        result.Add("f.write('" + filename + "',atob('" + Btoa(buff) + "')," + zhCurrent + ");");
                        break;
                    }

                }
            }

            return result;
        }


        public void SaveCode(string port, string filename, string code, bool restart = false)
        {
            ArrayList list = BuildFileWriteCode(filename, code);

            foreach (object val in list)
            {
                SendCode(port, val.ToString());
            }

            if (restart)
            {
                SendCode(port, "E.reboot();");
            }
        }

        public void ClosePort()
        {
            if (this.sp.IsOpen)
            {
                this.sp.Close();
            }

        }
        public void OpenPort(string port)
        {
            try
            {
                if (port == this.sp.PortName)
                {
                    if (!this.sp.IsOpen)
                    {
                        this.sp.Open();
                    }
                }
                else
                {
                    if (this.sp.IsOpen)
                    {
                        this.sp.Close();
                    }
                    this.sp.PortName = port;
                    this.sp.Open();
                }
            }
            catch (Exception)
            {
                throw new Exception("端口打开失败");
            }


        }

        public bool IsOpen()
        {
            return this.sp.IsOpen;
        }

        public void SetBootCode(string port, string code)
        {
            SaveCode(port, ".bootcde", code, true);
        }

        public void SendFile(string port, string filename, string code, bool restart = false)
        {
            SaveCode(port, filename, code, restart);
        }



    }
}
