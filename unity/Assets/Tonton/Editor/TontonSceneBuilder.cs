using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tonton.EditorTools
{
    /// <summary>
    /// メニュー「Tonton > Build PoC Scene」でPoC用シーンを組み立てて保存する．
    /// 寸法は実物の約10倍（土俵の板 3m 四方，力士の高さ 0.8m）．
    /// </summary>
    public static class TontonSceneBuilder
    {
        private const string Root = "Assets/Tonton";
        private const string MatDir = Root + "/Materials";
        private const string ScenePath = Root + "/Scenes/TontonPoC.unity";

        [MenuItem("Tonton/Build PoC Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(MatDir);
            EnsureFolder(Root + "/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // カメラ：正面（+x 側）から見る．画面左が東（-z），右が西（+z）
            var cam = Camera.main;
            cam.transform.position = new Vector3(3.2f, 2.0f, 0f);
            cam.transform.LookAt(new Vector3(0f, 0.35f, 0f));
            cam.fieldOfView = 45f;

            // 床（落ちた力士の受け）
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -1f, 0f);
            floor.transform.localScale = Vector3.one * 3f;
            Paint(floor, "Floor", new Color(0.25f, 0.22f, 0.2f));

            // 土俵の板（箱のふた）
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.position = Vector3.zero;
            board.transform.localScale = new Vector3(3f, 0.1f, 3f);
            Paint(board, "Board", new Color(0.85f, 0.75f, 0.55f));
            board.AddComponent<Rigidbody>();
            var boardCtrl = board.AddComponent<BoardController>();

            // 俵の円（見た目だけ，当たり判定なし）．板の子にして一緒に揺らす
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(board.transform, false);
            ring.transform.localScale = new Vector3(2.4f / 3f, 0.05f, 2.4f / 3f);   // 半径 1.2m
            ring.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Paint(ring, "Ring", new Color(0.75f, 0.6f, 0.4f));

            // 力士（板の上面 y=0.05 から少し浮かせて置き，めり込みによる弾きを避ける）
            // 東：-z から +z を向く，西：+z から -z を向く
            const float apexZ = 0.27f;
            var east = BuildWrestler("East", 1, new Vector3(0f, 0.06f, -apexZ), Quaternion.identity, new Color(0.8f, 0.2f, 0.2f));
            var west = BuildWrestler("West", 2, new Vector3(0f, 0.06f, apexZ), Quaternion.Euler(0f, 180f, 0f), new Color(0.2f, 0.3f, 0.8f));

            // 受信と進行
            var sys = new GameObject("TontonSystem");
            var receiver = sys.AddComponent<TonReceiver>();
            var match = sys.AddComponent<MatchManager>();
            match.receiver = receiver;
            match.board = boardCtrl;
            match.east = east;
            match.west = west;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Tonton] シーンを作成しました: {ScenePath}");
        }

        /// <summary>
        /// 紙の力士を作る．横から見て逆V字の脚板2枚と，相手に向けて伸ばした腕1枚．
        /// ルートの原点が足元の中心で，ローカル +z が前．
        /// </summary>
        private static Wrestler BuildWrestler(string name, int player, Vector3 pos, Quaternion rot, Color color)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(pos, rot);

            const float height = 0.8f;
            const float width = 0.5f;
            const float thick = 0.03f;
            const float halfAngle = 15f;
            float legLen = height / Mathf.Cos(halfAngle * Mathf.Deg2Rad);
            float footOffset = height * Mathf.Tan(halfAngle * Mathf.Deg2Rad) * 0.5f;

            // 前脚板：上端が中心，下端が前に開く
            AddPanel(root, "LegFront", new Vector3(0f, height * 0.5f, footOffset),
                Quaternion.Euler(-halfAngle, 0f, 0f), new Vector3(width, legLen, thick), color);
            // 後脚板
            AddPanel(root, "LegBack", new Vector3(0f, height * 0.5f, -footOffset),
                Quaternion.Euler(halfAngle, 0f, 0f), new Vector3(width, legLen, thick), color);
            // 腕：肩の高さから前へ．相手の腕とぶつかって組み合う
            AddPanel(root, "Arms", new Vector3(0f, height * 0.75f, 0.13f),
                Quaternion.identity, new Vector3(width * 0.9f, thick, 0.26f), color * 0.85f);
            // 頭（見た目と重心を少し上げる）
            AddPanel(root, "Head", new Vector3(0f, height + 0.08f, 0f),
                Quaternion.identity, new Vector3(0.18f, 0.16f, thick), color);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            var w = root.AddComponent<Wrestler>();
            w.player = player;
            return w;
        }

        private static void AddPanel(GameObject parent, string name, Vector3 localPos, Quaternion localRot, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = size;
            Paint(go, parent.name + "_" + name, color);
        }

        // AssetDatabase に認識される形でフォルダを作る
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        // 色ごとにマテリアルをアセットとして保存して割り当てる（Built-in / URP どちらでも既定シェーダを流用）
        private static void Paint(GameObject go, string matName, Color color)
        {
            var r = go.GetComponent<Renderer>();
            string path = $"{MatDir}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(r.sharedMaterial);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            EditorUtility.SetDirty(mat);
            r.sharedMaterial = mat;
        }
    }
}
