using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tonton
{
    /// <summary>
    /// 1番の進行と勝敗判定．
    /// 準備 → はっけよい（カウントダウン） → のこった（取組） → 決着．Spaceで次の一番．
    /// デバッグ用に F で東，J で西のトンを入力できる．
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public enum State { Ready, Countdown, Fighting, Result }

        public TonReceiver receiver;
        public BoardController board;
        public Wrestler east;   // player 1
        public Wrestler west;   // player 2

        [Header("ルール")]
        public float ringRadius = 1.2f;
        public float boutSeconds = 30f;
        public float countdownSeconds = 3f;

        [Header("物理設定（薄い紙を安定させる）")]
        public float fixedTimestep = 0.005f;

        [Header("デバッグ入力")]
        public bool keyboardDebug = true;
        public float keyboardStrength = 0.8f;

        public State state { get; private set; } = State.Ready;

        private float timer;
        private string resultText = "";
        private readonly int[] tons = new int[3];
        private readonly float[] lastStrength = new float[3];
        private int eastWins, westWins;
        private GUIStyle big, small;

        private void Awake()
        {
            Time.fixedDeltaTime = fixedTimestep;
            Physics.defaultSolverIterations = 16;
            Physics.defaultSolverVelocityIterations = 4;
        }

        private void OnEnable() { if (receiver) receiver.OnTon += HandleTon; }
        private void OnDisable() { if (receiver) receiver.OnTon -= HandleTon; }

        private void Start() { ResetBout(); }

        private void HandleTon(TonEvent ev)
        {
            lastStrength[ev.p] = (float)ev.s;
            if (state != State.Fighting) return;   // 取組中だけ土俵に効かせる
            tons[ev.p]++;
            board.Tap(ev.p, (float)ev.s);
        }

        private void Update()
        {
            if (keyboardDebug && receiver)
            {
                if (Pressed('F')) receiver.Inject(1, keyboardStrength);
                if (Pressed('J')) receiver.Inject(2, keyboardStrength);
            }

            switch (state)
            {
                case State.Ready:
                    if (Pressed(' ')) StartCountdown();
                    break;
                case State.Countdown:
                    timer -= Time.deltaTime;
                    if (timer <= 0f) { state = State.Fighting; timer = boutSeconds; }
                    break;
                case State.Fighting:
                    timer -= Time.deltaTime;
                    Judge();
                    if (state == State.Fighting && timer <= 0f) Finish(0, "時間切れ　取り直し");
                    break;
                case State.Result:
                    if (Pressed(' ')) { ResetBout(); StartCountdown(); }
                    break;
            }
        }

        private void Judge()
        {
            Vector3 c = board.transform.position;
            bool eastLost = east.HasFallen() || east.IsOutside(c, ringRadius);
            bool westLost = west.HasFallen() || west.IsOutside(c, ringRadius);

            if (eastLost && westLost) Finish(0, "同体　取り直し");
            else if (eastLost) Finish(2, $"西の勝ち（東の{Reason(east)}）");
            else if (westLost) Finish(1, $"東の勝ち（西の{Reason(west)}）");
        }

        private static string Reason(Wrestler loser) => loser.HasFallen() ? "倒れ" : "土俵外";

        private void Finish(int winner, string text)
        {
            state = State.Result;
            if (winner == 1) eastWins++;
            if (winner == 2) westWins++;
            resultText = text;
        }

        private void StartCountdown()
        {
            state = State.Countdown;
            timer = countdownSeconds;
        }

        private void ResetBout()
        {
            board.ResetBoard();
            east.ResetPose();
            west.ResetPose();
            tons[1] = tons[2] = 0;
            resultText = "";
            state = State.Ready;
        }

        private void OnGUI()
        {
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
                small = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            }

            float w = Screen.width;
            GUI.Label(new Rect(20, 20, 420, 140),
                $"東  {eastWins}勝　トン {tons[1]}　強さ {lastStrength[1]:0.00}　{Link(1)}\n" +
                $"西  {westWins}勝　トン {tons[2]}　強さ {lastStrength[2]:0.00}　{Link(2)}", small);

            string center = state switch
            {
                State.Ready => "Spaceで はっけよい",
                State.Countdown => $"はっけよい… {Mathf.CeilToInt(timer)}",
                State.Fighting => $"のこった！ {Mathf.CeilToInt(timer)}",
                State.Result => $"{resultText}\nSpaceで次の一番",
                _ => ""
            };
            GUI.Label(new Rect(0, 40, w, 140), center, big);

            if (keyboardDebug)
                GUI.Label(new Rect(20, Screen.height - 40, 600, 30), "デバッグ：F=東のトン，J=西のトン", small);
        }

        // 新旧どちらの Input 設定でも動くようにする
        private static bool Pressed(char key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return key switch
            {
                'F' => kb.fKey.wasPressedThisFrame,
                'J' => kb.jKey.wasPressedThisFrame,
                ' ' => kb.spaceKey.wasPressedThisFrame,
                _ => false
            };
#else
            return key switch
            {
                'F' => Input.GetKeyDown(KeyCode.F),
                'J' => Input.GetKeyDown(KeyCode.J),
                ' ' => Input.GetKeyDown(KeyCode.Space),
                _ => false
            };
#endif
        }

        private string Link(int p)
        {
            if (!receiver) return "";
            float t = receiver.lastReceived[p];
            if (t <= 0f) return "未受信";
            return Time.time - t < 5f ? "受信中" : "途絶";
        }
    }
}
