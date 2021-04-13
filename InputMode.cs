using System;
using System.Diagnostics;
using System.IO;

namespace espjs
{
    /// <summary>
    /// 输入代码模式, 可以用于临时粘贴代码运行
    /// </summary>
    public class InputMode
    {
        public static void Run(Uart uart, string port)
        {
            string code = "";
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
        }
    }
}
