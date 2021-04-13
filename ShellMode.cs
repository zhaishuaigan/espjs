using System;
using System.Diagnostics;
using System.IO;

namespace espjs
{
    /// <summary>
    /// 命令行模式
    /// </summary>
    public class ShellMode
    {
        public static void Run(Uart uart, string port)
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
    }
}
