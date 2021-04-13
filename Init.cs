using System;
using System.Diagnostics;
using System.IO;

namespace espjs
{
    /// <summary>
    /// 初始化项目操作类
    /// </summary>
    public class Init
    {
        public static void Run(string workDir, string execDir)
        {
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
        }
    }
}
