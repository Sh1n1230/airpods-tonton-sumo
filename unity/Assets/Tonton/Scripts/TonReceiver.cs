using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Tonton
{
    /// <summary>iPhoneから届く「トン」イベント．iOS側の TonEvent と同じ形．</summary>
    [Serializable]
    public class TonEvent
    {
        public int p;     // プレイヤーID（1:東，2:西）
        public int seq;   // 連番
        public double t;  // 送信側時刻（秒）
        public double s;  // 強さ 0..1
    }

    /// <summary>
    /// UDPでトンを受け取り，メインスレッドで OnTon を発火する．
    /// 受信は別スレッド，ゲームへの反映は Update で行う．
    /// </summary>
    public class TonReceiver : MonoBehaviour
    {
        public int port = 9000;

        public event Action<TonEvent> OnTon;

        /// <summary>プレイヤーごとの最終受信時刻（Time.time）．画面表示用．</summary>
        public float[] lastReceived = new float[3];

        private UdpClient client;
        private Thread thread;
        private volatile bool running;
        private readonly ConcurrentQueue<TonEvent> queue = new ConcurrentQueue<TonEvent>();
        private readonly int[] lastSeq = new int[3];

        private void OnEnable()
        {
            try
            {
                client = new UdpClient(port);
            }
            catch (SocketException e)
            {
                Debug.LogError($"[TonReceiver] ポート {port} を開けませんでした: {e.Message}");
                return;
            }
            running = true;
            thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "TonReceiver" };
            thread.Start();
            Debug.Log($"[TonReceiver] UDP {port} で待ち受け中");
        }

        private void OnDisable()
        {
            running = false;
            client?.Close();   // Receive のブロックを解く
            client = null;
            thread = null;
        }

        private void ReceiveLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                try
                {
                    byte[] data = client.Receive(ref remote);
                    var ev = JsonUtility.FromJson<TonEvent>(Encoding.UTF8.GetString(data));
                    if (ev != null && (ev.p == 1 || ev.p == 2)) queue.Enqueue(ev);
                }
                catch (SocketException) { /* Close() による終了 */ }
                catch (ObjectDisposedException) { break; }
                catch (Exception e) { Debug.LogWarning($"[TonReceiver] 不正なパケット: {e.Message}"); }
            }
        }

        private void Update()
        {
            while (queue.TryDequeue(out var ev))
            {
                // 重複・逆順の破棄．iPhone側の再起動で連番が大きく戻った場合は受け入れる
                int last = lastSeq[ev.p];
                if (ev.seq <= last && last - ev.seq < 50) continue;
                lastSeq[ev.p] = ev.seq;

                lastReceived[ev.p] = Time.time;
                OnTon?.Invoke(ev);
            }
        }

        /// <summary>キーボードなどからのデバッグ入力用．</summary>
        public void Inject(int player, float strength)
        {
            OnTon?.Invoke(new TonEvent { p = player, seq = -1, t = Time.timeAsDouble, s = strength });
        }
    }
}
