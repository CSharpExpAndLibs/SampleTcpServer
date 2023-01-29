using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

namespace Client
{
    class Client
    {
        static bool IsValid(string cmd)
        {
            switch (cmd)
            {
                case "StartPrint":
                case "StopPrint":
                case "FinishProcess":
                case "test":
                    break;
                default:
                    return false;
            }
            return true;
        }

        static void Usage()
        {
            Console.WriteLine("Usage:Client [-h][-p port]");
            Console.WriteLine("  -p port    :portを指定する(Default = 12345)");
            Console.WriteLine("  -h         :使い方を表示して終了");
        }

        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                Usage();
                return;
            }

            int port = 12345;
            if (args.Length == 2)
            {
                if (args[0] != "-p")
                {
                    Usage();
                    return;
                }
                if (!int.TryParse(args[1], out port))
                {
                    Usage();
                    return;
                }
            }

            TcpClient client = null;
            try
            {
                client = new TcpClient("127.0.0.1", port);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ホストへの接続に失敗しました ip = '127.0.0.1'、port = {port}:例外:{e.Message}");
                return;
            }

            while (true)
            {
                Console.WriteLine("送信する文字列を入力して下さい");
                // TODO 23/1/28
            }
            Console.WriteLine("Clientを終了します。何かキーを押して下さい。");
            Console.ReadLine();
        }
    }
}
