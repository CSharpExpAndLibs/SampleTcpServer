using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClientManagement;

/// <summary>
/// 次の仕様を持つサーバーをBeginAcceptTcpClientを使って実装する。<br/>
/// 1. 指定されたポートをリッスンし、接続したクライアント毎に個別のタスクで応答する。<br/>
/// 2. クライアントから受け取った文字列を大文字に変換してクライアントへ返すと共に
/// 文字列をコンソールに表示する。<br/>
/// 3. クライアントから'TermServer'を受け取ったら接続中のクライアントを全て切断してから
/// リスナを閉じて終了する。<br/>
/// 4. コンソールから'Quit'を入力すると3.と同様に終了する。<br/>
/// </summary>
namespace BeginListenerSample
{
    class Program
    {
        static int port = 12345;

        static void Usage()
        {
            Console.WriteLine("Usage:BeginListenerSample [-p port]");
            Console.WriteLine(" Option:");
            Console.WriteLine("     -p port: リッスンポート指定（デフォルト=12345）");
        }
        static void Main(string[] args)
        {
            if (args.Length != 0 && args.Length != 2)
            {
                Usage();
                return;
            }

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
        }

        static void StartServer(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            server.Start();

            while (true)
            {

                Console.WriteLine("Main:Accept開始");
                var r = server.BeginAcceptTcpClient(new AsyncCallback(SampleCallback), server);
                Console.WriteLine("Main:Acceptから返った。r.WaitHandle監視開始");
                r.AsyncWaitHandle.WaitOne();
                Console.WriteLine("Main:r.WatiHandle検出");
            }
        }

        static void SampleCallback(IAsyncResult result)
        {
            Console.WriteLine("SampleCallback:Enter");

            TcpClient client = null;
            try
            {
                client = ((TcpListener)result.AsyncState).EndAcceptTcpClient(result);
            }
            catch
            {
                // BeginAcceptの親クラスであるListenerが閉じられると
                // このCallbackが呼ばれる。この条件でEndAcceptを呼ぶと
                // 例外が起きるので、この例のようにcatchすること。
                // これを知らないと間違いなく嵌ります。
            }
            var stream = client.GetStream();
            var readData = new byte[256];

            // Clientが接続を切るまで処理を続ける
            while (true)
            {
                try
                {
                    var len = stream.Read(readData, 0, readData.Length);
                    if (len == 0)
                    {
                        Console.WriteLine("SampleCallback:streamが閉じられた");
                        break;
                    }

                    var str = Encoding.UTF8.GetString(readData, 0, len);

                    Console.WriteLine($"SampleCallback:Recieved '{str}'");

                    var writeData = Encoding.UTF8.GetBytes(str.ToUpper());
                    stream.Write(writeData, 0, writeData.Length);
                }
                catch (Exception ep)
                {
                    Console.WriteLine($"SampleCallback:Clientから切断された:{ep.Message}");
                    break;
                }
            }

            client.Dispose();
            Console.WriteLine("SampleCallback:Exit");
        }
    }
}
