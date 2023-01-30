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
        static TcpListener tcpServer = null;
        static Task serverTask = null;
        static ClientManager clientManager;
        static AutoResetEvent termServerEvent;

        static void Main(string[] args)
        {
            if (args.Length != 0 && args.Length != 2)
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
            // クライアントマネージャーの生成
            clientManager = new ClientManager();

            // Serverを起動する
            termServerEvent = new AutoResetEvent(false);
            if (!StartServer(port))
            {
                Console.WriteLine("サーバー起動に失敗しました");
                goto END;
            }
            Console.WriteLine($"Serverを起動しました。listen port={port}");

            while (true)
            {
                Console.WriteLine("終了するには'Quit'を入力して下さい。");
                var input = Console.ReadLine();
                if (input == "Quit")
                {
                    TermServer();
                    break;
                }
            }

        END:
            Console.WriteLine("何かキーを押して下さい");
            Console.ReadLine();
        }

        static void Usage()
        {
            Console.WriteLine("Usage:BeginListenerSample [-p port]");
            Console.WriteLine(" Option:");
            Console.WriteLine("     -p port: リッスンポート指定（デフォルト=12345）");
        }


        static bool StartServer(int port)
        {
            try
            {
                tcpServer = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                tcpServer.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"サーバーの起動に失敗しました:例外発生:{e.Message}");
                return false;
            }

            serverTask = Task.Run(() =>
            {
                while (true)
                {
                    var r = tcpServer.BeginAcceptTcpClient(new AsyncCallback(CommunicateWithClient), tcpServer);
                    var evtIdx = WaitHandle.WaitAny(new WaitHandle[] { r.AsyncWaitHandle, termServerEvent });
                    if (evtIdx == 0)
                        continue;

                    // ---- Serverの終了処理 ----
                    // 全てのクライアントを閉じる
                    lock (clientManager)
                        clientManager.RemoveAll();

                    tcpServer.Stop();
                    break;
                }
            });

            // serverTaskへのタスクスイッチ
            Thread.Sleep(0);

            return true;
        }

        static void CommunicateWithClient(IAsyncResult result)
        {
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
                return;
            }

            // clientManagerへ登録
            lock (clientManager)
                clientManager.Add(client);

            int clientId = clientManager.GetClientId(client);
            Console.WriteLine($"新しいクライアントを開始しました:ClientId={clientId}");

            var stream = client.GetStream();
            var readData = new byte[256];

            // 相手が接続を切るか終了命令を受け取るまで処理を続ける
            while (true)
            {
                try
                {
                    var len = stream.Read(readData, 0, readData.Length);
                    if (len == 0)
                    {
                        Console.WriteLine($"クライアント[{clientId}]は接続を切断されました");
                        break;
                    }
                    var str = Encoding.UTF8.GetString(readData, 0, len);

                    Console.WriteLine($"クライアント[{clientId}]は'{str}'を受け取りました");
                    if (str == "TermServer")
                    {
                        Console.WriteLine($"クライアント[{clientId}]は終了命令を受け取りました");
                        TermServer();
                        break;
                    }

                    var writeData = Encoding.UTF8.GetBytes(str.ToUpper());
                    stream.Write(writeData, 0, writeData.Length);
                }
                catch
                {
                    Console.WriteLine($"クライアント[{clientId}]は接続を切断されました");
                    break;
                }
            }

            lock (clientManager)
                clientManager.Remove(client);
        }

        static void TermServer()
        {
            termServerEvent.Set();
            if (!serverTask.Wait(10000))
            {
                Console.WriteLine("TermServer:Timeoutしたけどどうしょうもないよね～");
                return;
            }
            Console.WriteLine("TermServer:Serverを終了しました。");
        }
    }
}
