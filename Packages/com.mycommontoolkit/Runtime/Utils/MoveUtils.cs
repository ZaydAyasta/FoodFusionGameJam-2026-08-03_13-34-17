using UnityEngine;
using UnityEngine.Windows;

namespace MyCommonToolkit
{
    namespace Utils2D {
        public static class MoveUtils
        {
            public static Vector2 MoveLerp(Vector2 current, Vector2 target, ref float t, float speed, AnimationCurve curve)
            {
                t += speed * Time.deltaTime / 100;
                t = Mathf.Clamp01(t);
                return Vector2.Lerp(current, target, curve.Evaluate(t));
            }
            public static void RbMovement(Rigidbody2D rb, float direction, float maxSpeed, float acceleration, float deceleration)
            {
                Vector2 velocity = rb.linearVelocity;
                float targetSpeed = direction * maxSpeed;
                float speedDiff = targetSpeed - velocity.x;
                float rate =Mathf.Abs(direction) > 0.01f? acceleration: deceleration;
                float force = speedDiff * rate;
                rb.AddForce(Vector2.right * force, ForceMode2D.Force);
            }
            public static void RbMovement(Rigidbody2D rb, float direction, float maxSpeed, float acceleration, float deceleration,bool isGrounded,float airControl)
            {
                float control=isGrounded ? 1f : airControl;
                float controlledAccel=acceleration * control;

                Vector2 velocity = rb.linearVelocity;
                float targetSpeed = direction * maxSpeed;
                float speedDiff = targetSpeed - velocity.x;
                float rate = Mathf.Abs(direction) > 0.01f ? controlledAccel : deceleration;
                float force = speedDiff * rate;
                rb.AddForce(Vector2.right * force, ForceMode2D.Force);
            }
        }
    }
}
