using UnityEngine;

namespace MyCommonToolkit
{
    namespace Utils3D
    {
        public class MoveUtils3D : MonoBehaviour
        {
            public static Vector3 MoveLerp(Vector3 current, Vector3 target, ref float t, float speed, AnimationCurve curve)
            {
                t += speed * Time.deltaTime / 100;
                t = Mathf.Clamp01(t);
                return Vector2.Lerp(current, target, curve.Evaluate(t));
            }
            public static void RbMovement(Rigidbody rb, float direction, float maxSpeed, float acceleration, float deceleration)
            {
                Vector3 velocity = rb.linearVelocity;
                float targetSpeed = direction * maxSpeed;
                float speedDiff = targetSpeed - velocity.x;
                float rate = Mathf.Abs(direction) > 0.01f ? acceleration : deceleration;
                float force = speedDiff * rate;
                rb.AddForce(Vector3.right * force, ForceMode.Force);
            }
            public static void RbMovement(Rigidbody rb, float direction, float maxSpeed, float acceleration, float deceleration, bool isGrounded, float airControl)
            {
                float control = isGrounded ? 1f : airControl;
                float controlledAccel = acceleration * control;

                Vector3 velocity = rb.linearVelocity;
                float targetSpeed = direction * maxSpeed;
                float speedDiff = targetSpeed - velocity.x;
                float rate = Mathf.Abs(direction) > 0.01f ? controlledAccel : deceleration;
                float force = speedDiff * rate;
                rb.AddForce(Vector3.right * force, ForceMode.Force);
            }
        }
    }
}
