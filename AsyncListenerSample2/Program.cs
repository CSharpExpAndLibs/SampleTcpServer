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
/// 次の仕様を持つサーバーをAcceptTcpClientAsyncを使って実装する。<br/>
/// 1. 指定されたポートをリッスンし、接続したクライアント毎に個別のタスクで応答する。<br/>
/// 2. クライアントから'GetClientId'を受け取ったらクライアントIDを返す。<br/>
/// 3. クライアントから受け取った文字列を大文字に変換してクライアントへ返すと共に
/// 文字列をコンソールに表示する。<br/>
/// 4. クライアントから'TermServer'を受け取ったら接続中のクライアントを全て切断してから
/// リスナを閉じて終了する。<br/>
/// 5. コンソールから'Quit'を入力すると4.と同様に終了する。<br/>
/// 
/// AsyncListenerSampleをベースにして、async ~ await スタイルで書き直す。
/// </summary>

namespace AsyncListenerSample2
{
    class Program
    {
        static TcpListener tcpServer = null;
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
            try
            {
                tcpServer = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                termServerEvent = new AutoResetEvent(false);


                StartServerAsync(port);
                Console.WriteLine($"Serverを起動しました。listen port={port}");

                while (true)
                {
                    Console.WriteLine("終了するには'Quit'を入力して下さい。");
                    var input = Console.ReadLine();
                    if (input == "Quit")
                    {
                        termServerEvent.Set();
                        break;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Server起動に失敗しました。");
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
        /// <summary>
        /// while(){}の中でawaitしても成り立つことにちょっと驚いた。
        /// これが出来なければまともなasync~await形式にはならなかった。
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        static async Task StartServerAsync(int port)
        {
            // サーバーリッスン開始
            tcpServer.Start();

            // サーバー停止タスク
            Task<TcpClient> termServerTask = Task.Run(() => { termServerEvent.WaitOne(); return (TcpClient)null; });

            while (true)
            {

                try
                {
                    // AcceptかTermか、先に完了した方を選択する
                    var serverTask = tcpServer.AcceptTcpClientAsync();
                    var task = await Task.WhenAny(new Task<TcpClient>[] { serverTask, termServerTask });

                    TcpClient client = task.Result;


                    // --- サーバー停止処理 ---
                    if (task == termServerTask)
                    {
                        // 全てのクライアントを閉じる
                        lock (clientManager)
                            clientManager.RemoveAll();

                        tcpServer.Stop();
                        break;
                    }

                    // --- 通常のサーバー処理 ---

                    // client通信処理開始
                    Task.Run(() => CommunicateWithClient(client));

                }
                catch (Exception e)
                {
                    Console.WriteLine($"tcpServerが終了しました。例外={e.Message}");
                }
            }
            Console.WriteLine("tcpServerが終了しました。");
        }

        static void CommunicateWithClient(TcpClient client)
        {
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

                    string sendMsg = null;
                    if (str == "TermServer")
                    {
                        Console.WriteLine($"クライアント[{clientId}]は終了命令を受け取りました");
                        termServerEvent.Set();
                        break;
                    }
                    else if (str == "GetClientId")
                    {
                        sendMsg = clientId.ToString();
                    }
                    else
                    {
                        sendMsg = str.ToUpper();
                    }

                    var writeData = Encoding.UTF8.GetBytes(sendMsg);
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
    }
}
