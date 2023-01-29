using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
            Console.WriteLine("Usage:Client [-h][-m [[PrintLifeTime][ForcePrintError]]]");
            Console.WriteLine("  -m:MainControllを起動する");
            Console.WriteLine("      PrintLifeTime      :MainControllのプリントタスクの持続時間");
            Console.WriteLine("      ForcePrintError    :MainControllのプリントタスクを起動時に強制的にエラー終了する");
            Console.WriteLine("  -h:使い方を表示して終了");
        }

        static void Main(string[] args)
        {
            string printLifeTime = "0";
            string forcePrintError = "0";
            if (args.Length != 0)
            {
                if (args[0] == "-h" || args[0] != "-m")
                {
                    Usage();
                    return;
                }
                if (args.Length >= 2)
                    printLifeTime = args[1];
                if (args.Length >= 3)
                    forcePrintError = args[2];

                // MainControll Processを起動
                string exe = @"C:\Users\chara\source\repos\SampleController" +
                    @"\MainControll\bin\Debug\MainControll.exe";
                string arguments = $"{printLifeTime} {forcePrintError}";
                Process.Start(exe, arguments);
            }
            while (true)
            {
                Console.WriteLine("送信する文字列を入力して下さい");
                var cmd = Console.ReadLine().Trim();
                if (!IsValid(cmd))
                {
                    Console.WriteLine($"{cmd}はサポート外です");
                    continue;
                }

                try
                {
                    var client = new TcpClient("127.0.0.1", 12345);

                    Console.WriteLine("接続しました");
                    var s = client.GetStream();
                    // 送付
                    var sdata = Encoding.UTF8.GetBytes(cmd);
                    s.Write(sdata, 0, sdata.Length);
                    Console.WriteLine("送信しました");

                    // レスポンス受信
                    byte[] rdata = new byte[256];
                    var c = s.Read(rdata, 0, rdata.Length);
                    var response = Encoding.UTF8.GetString(rdata, 0, c);

                    // レスポンスの分解
                    var respondes = response.Split(new char[] { ':' });
                    foreach (var str in respondes)
                    {
                        Console.WriteLine(str);
                    }
                    s.Close(); client.Close(); client.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (cmd == "FinishProcess")
                    break;
            }
            Console.WriteLine("Clientを終了します。何かキーを押して下さい。");
            Console.ReadLine();
        }
    }
}
