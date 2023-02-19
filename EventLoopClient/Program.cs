using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventLoopClient
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient cl = new TcpClient("127.0.0.1", 12345);
            var s = cl.GetStream();

            while (true)
            {
                Console.WriteLine("コマンド入力?");
                var cmd = Console.ReadLine();
                switch (cmd.ToLower())
                {
                    case "s1":
                        cmd = "GotoState1:おまけ";
                        break;
                    case "s2":
                        cmd = "GotoState2:おまけ";
                        break;
                    case "s3":
                        cmd = "GotoState3:おまけ";
                        break;
                    case "s4":
                        cmd = "GotoState4:おまけ";
                        break;
                    case "f":
                        cmd = "FinishProcess";
                        break;
                    default:
                        continue;
                }
                var d = Encoding.UTF8.GetBytes(cmd);

                try
                {
                    s.Write(d, 0, d.Length);

                    byte[] rcvData = new byte[256];
                    var c = s.Read(rcvData, 0, rcvData.Length);
                    if (c == 0)
                        break;
                    Console.WriteLine($"受信メッセージ：" +
                        $"{Encoding.UTF8.GetString(rcvData, 0, c)}");
                }
                catch
                {
                    break;
                }
            }
            Console.WriteLine("切断されました。");
            Console.WriteLine("Press Enter!");
            Console.ReadLine();
        }
    }
}
