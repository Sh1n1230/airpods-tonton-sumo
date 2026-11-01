using UnityEngine;

namespace Tonton
{
    /// <summary>
    /// 紙の力士．子オブジェクトの BoxCollider（前後の脚板と腕）で形を作り，ルートに Rigidbody を持つ．
    /// 薄く軽い物体はPhysXで不安定になりやすいので，ワールドは実物の約10倍で作っている．
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Wrestler : MonoBehaviour
    {
        public int player = 1;
        [Tooltip("この角度以上傾いたら倒れ")]
        public float fallAngle = 55f;

        public float friction = 0.7f;

        private Rigidbody rb;
        private Vector3 startPos;
        private Quaternion startRot;

        public Rigidbody Body => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.solverIterations = 16;
            rb.solverVelocityIterations = 4;
            startPos = transform.position;
            startRot = transform.rotation;

#if UNITY_6000_0_OR_NEWER
            var mat = new PhysicsMaterial("Paper")
            {
                dynamicFriction = friction,
                staticFriction = friction,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
#else
            var mat = new PhysicMaterial("Paper")
            {
                dynamicFriction = friction,
                staticFriction = friction,
                bounciness = 0f,
                frictionCombine = PhysicMaterialCombine.Average,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
#endif
            foreach (var col in GetComponentsInChildren<Collider>()) col.sharedMaterial = mat;
        }

        public bool HasFallen()
        {
            return Vector3.Angle(transform.up, Vector3.up) > fallAngle;
        }

        /// <summary>土俵の中心から水平距離 radius を超えたら土俵外．</summary>
        public bool IsOutside(Vector3 ringCenter, float radius)
        {
            Vector3 d = rb.worldCenterOfMass - ringCenter;
            d.y = 0f;
            return d.magnitude > radius;
        }

        public void ResetPose()
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.position = startPos;
            rb.rotation = startRot;
            transform.SetPositionAndRotation(startPos, startRot);
        }
    }
}
