using System;

namespace TestApp
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("测试程序启动成功！");
            Console.WriteLine("按任意键继续...");
            Console.ReadKey();
            
            Console.WriteLine("程序即将退出...");
            System.Threading.Thread.Sleep(2000);
        }
    }
}
