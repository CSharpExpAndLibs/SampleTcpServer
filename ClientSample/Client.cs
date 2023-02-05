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
                goto END;
            }

            int port = 12345;
            if (args.Length == 2)
            {
                if (args[0] != "-p")
                {
                    Usage();
                    goto END;
                }
                if (!int.TryParse(args[1], out port))
                {
                    Usage();
                    goto END;
                }
            }

            TcpClient client = null;
            try
            {
                client = new TcpClient("127.0.0.1", port);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ホストへの接続に失敗しました ip = '127.0.0.1'," +
                    $"port = {port}(例外発生)");
                goto END;
            }

            Console.WriteLine($"ホストへ接続しました ip = '127.0.0.1'、port = {port}");

            // クライアントIDを取得する
            var cidStr = GetRecievMsg(client, "GetClientId");
            if (cidStr == null)
            {
                Console.WriteLine("ホストから切断されました。");
                goto END;
            }
            int cid;
            if (!int.TryParse(cidStr, out cid))
            {
                Console.WriteLine("ホストはGetCliendIdコマンドに未対応です。");
            }
            else
            {
                Console.WriteLine($"クライアントIDは{cid}です。");
            }

            // Consoleから入力した文字列をサーバーへ送り
            // 受信文字列を受け取る
            while (true)
            {
                Console.WriteLine("送信する文字列を入力して下さい。" +
                    "'TermServer'を入力するとサーバーが終了します。");

                var inStr = Console.ReadLine();
                if (string.IsNullOrEmpty(inStr))
                    continue;

                var rcvMsg = GetRecievMsg(client, inStr);
                if (rcvMsg == null)
                {
                    Console.WriteLine("ホストから切断されました。");
                    goto END;
                }
                Console.WriteLine($"受信メッセージ={rcvMsg}");
            }

        END:
            Console.WriteLine("Clientを終了します。何かキーを押して下さい。");
            Console.ReadLine();
        }

        static string GetRecievMsg(TcpClient client, string sendMsg)
        {
            var stream = client.GetStream();
            var sendData = Encoding.UTF8.GetBytes(sendMsg);
            var recData = new byte[256];
            try
            {
                stream.Write(sendData, 0, sendData.Length);
                var c = stream.Read(recData, 0, recData.Length);
                if (c == 0)
                {
                    return null;
                }
                return Encoding.UTF8.GetString(recData, 0, c);
            }
            catch
            {
                return null;
            }
        }
    }
}
