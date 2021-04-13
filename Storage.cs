using System;
using System.Diagnostics;
using System.IO;

namespace espjs
{
    /// <summary>
    /// 设备内部储存管理
    /// </summary>
    public class Storage
    {
        public static void Run(Uart uart, string port, string[] args)
        {
            string cmd = "list";
            if (args.Length <= 1)
            {
                cmd = args[0];
            }
            else
            {
                cmd = args[1];
            }
            switch (cmd)
            {
                case "ll":
                case "ls":
                case "dir":
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
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("请输入Storage的名称");
                        return;
                    }
                    uart.SendCode(port, "require('Storage').erase('" + args[2] + "')");
                    break;
                case "get":
                case "read":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("请输入Storage的名称");
                        return;
                    }
                    uart.SendCode(port, "console.log(require('Storage').read('" + args[2] + "'))");
                    break;
                case "save":
                case "write":
                    if (args.Length <= 3)
                    {
                        Console.WriteLine("Storage的名称或内容不能为空");
                        return;
                    }
                    uart.SendCode(port, "require('Storage').write('" + args[2] + "','" + args[3] + "')");
                    break;
            }
        }
    }
}
