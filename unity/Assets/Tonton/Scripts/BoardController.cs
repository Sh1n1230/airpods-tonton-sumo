using UnityEngine;

namespace Tonton
{
    /// <summary>
    /// 土俵（箱のふた）．バネとダンパで支えた板として，上下動（heave）と前後の傾き（pitch）を積分し，
    /// キネマティックRigidbodyとして動かす．板が動くことで接触している力士が揺れる．
    /// 東（1）は -z 側の縁，西（2）は +z 側の縁を叩く．
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoardController : MonoBehaviour
    {
        [Header("叩いたときの力（強さ1あたり）")]
        public float heaveImpulse = 1.2f;   // 下向き速度 [m/s]
        public float pitchImpulse = 60f;    // 角速度 [deg/s]
        [Tooltip("左右の傾きに混ぜる乱れ．本物の紙相撲の予測できなさを出す")]
        public float rollNoise = 15f;       // 角速度 [deg/s]

        [Header("バネ・ダンパ")]
        public float heaveStiffness = 900f;
        public float heaveDamping = 18f;
        public float tiltStiffness = 900f;
        public float tiltDamping = 18f;

        [Header("可動域")]
        public float maxHeave = 0.05f;   // [m]
        public float maxTilt = 4f;       // [deg]

        private Rigidbody rb;
        private Vector3 basePos;
        private Quaternion baseRot;

        private float heave, heaveVel;
        private float pitch, pitchVel;
        private float roll, rollVel;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            basePos = transform.position;
            baseRot = transform.rotation;
        }

        /// <summary>プレイヤー player が強さ strength（0..1）で縁を叩く．</summary>
        public void Tap(int player, float strength)
        {
            strength = Mathf.Clamp01(strength);
            float side = player == 1 ? -1f : 1f;   // 正のpitchで +z 側が下がる

            heaveVel -= heaveImpulse * strength;
            pitchVel += side * pitchImpulse * strength;
            rollVel += Random.Range(-1f, 1f) * rollNoise * strength;
        }

        public void ResetBoard()
        {
            heave = heaveVel = pitch = pitchVel = roll = rollVel = 0f;
            rb.position = basePos;
            rb.rotation = baseRot;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            Integrate(ref heave, ref heaveVel, heaveStiffness, heaveDamping, dt);
            Integrate(ref pitch, ref pitchVel, tiltStiffness, tiltDamping, dt);
            Integrate(ref roll, ref rollVel, tiltStiffness, tiltDamping, dt);

            ClampState(ref heave, ref heaveVel, maxHeave);
            ClampState(ref pitch, ref pitchVel, maxTilt);
            ClampState(ref roll, ref rollVel, maxTilt);

            rb.MovePosition(basePos + Vector3.up * heave);
            rb.MoveRotation(baseRot * Quaternion.Euler(pitch, 0f, roll));
        }

        // 半陰的オイラー法で減衰振動を積分する
        private static void Integrate(ref float x, ref float v, float k, float c, float dt)
        {
            v += (-k * x - c * v) * dt;
            x += v * dt;
        }

        // 可動域の端に当たったら止める（箱の縁に当たる感覚）
        private static void ClampState(ref float x, ref float v, float limit)
        {
            if (x > limit) { x = limit; if (v > 0) v = 0; }
            else if (x < -limit) { x = -limit; if (v < 0) v = 0; }
        }
    }
}
