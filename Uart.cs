using System;
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

        public int commandSplitTime = 100; // 每条命令间隔时间 (毫秒)
        public int codeSplitLen = 30; // 代码分割块长度
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

        public static byte[] HexStringToBytes(string hs)
        {
            string[] strArr = hs.Trim().Split(' ');
            byte[] b = new byte[strArr.Length];
            //逐个字符变为16进制字节数据
            for (int i = 0; i < strArr.Length; i++)
            {
                b[i] = Convert.ToByte(strArr[i], 16);
            }
            //按照指定编码将字节数组变为字符串
            return b;
        }

        public void SendHex(string port, string code)
        {
            SendCode(port, code, true);
        }



        /// <summary>
        /// 发送代码
        /// </summary>
        /// <param name="port"></param>
        /// <param name="code"></param>
        public void SendCode(string port, string code, bool hex = false)
        {
            try
            {
                OpenPort(port);
                code = code.Trim();
                // 这里需要暂停一下, 每个命令之间需要间隔一段时间, 否则容易造成单片机死机
                if (code == "sleep")
                {
                    Thread.Sleep(commandSplitTime);
                    return;
                }
                this.codeHistory.Add(code);
                if (this.codeHistory.Count > 50)
                {
                    this.codeHistory.RemoveAt(0);
                }

                if (hex)
                {
                    byte[] v = HexStringToBytes(code);
                    sp.Write(v, 0, v.Length);
                }
                else
                {
                    sp.WriteLine(code);
                }


                //sp.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("代码发送失败, 可能端口被占用了.");
            }

        }

        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string str = sp.ReadLine();
                str = new Regex("^>").Replace(str, "");
                str = new Regex("^:").Replace(str, "");
                str = str.TrimEnd();
                switch (str)
                {
                    case "":
                    case ">":
                    case " ":
                    case "=undefined":
                    case "=null":
                    case "=true":
                    case "=false":
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
            catch (FormatException)
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
            code = new Regex("\n +").Replace(code, "\n");
            code = code.Trim();

            int codeLen = GetZhLen(code);
            filename = filename.Trim();
            ArrayList result = new ArrayList
            {
                "global.f=require('Storage');",
                "sleep",
                "(_=>{global.c=_=>{if(f.getFree()<1000){console.log('Storage overflow');return false;}return true;}})();",
                "sleep",
                "(_=>{global.w=function(code,offset){c()&&f.write('" + filename + "',code,offset)}})();",
                "sleep",
            };

            // 这里设置每次写入的代码长度
            if (code.Length <= codeSplitLen)
            {
                result.Add("f.write('" + filename + "',atob('" + Btoa(code) + "'));");
                result.Add("sleep");
            }
            else
            {
                int current = 0;
                int zhCurrent = 0;
                string buff = code.Substring(current, codeSplitLen);
                int buffLen = GetZhLen(buff);
                result.Add("f.write('" + filename + "',atob('" + Btoa(buff) + "'),0," + codeLen + ");");
                current += codeSplitLen;
                zhCurrent += buffLen;
                while (true)
                {
                    result.Add("sleep");
                    if (code.Length >= current + codeSplitLen)
                    {
                        buff = code.Substring(current, codeSplitLen);
                        buffLen = GetZhLen(buff);
                        result.Add("w(atob('" + Btoa(buff) + "')," + zhCurrent + ");");
                        current += codeSplitLen;
                        zhCurrent += buffLen;
                    }
                    else
                    {
                        buff = code.Substring(current);
                        result.Add("w(atob('" + Btoa(buff) + "')," + zhCurrent + ");");
                        break;
                    }

                }
            }

            result.Add("f = null;");
            result.Add("w = null;");
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
