using UnityEngine;

namespace MyCommonToolkit
{
    namespace TopDown
    {
        public static class Movement
        {
            public static void Move(Rigidbody2D rb,Vector2 input, float speed)
            {
                rb.linearVelocity = speed * input.normalized;
            }
            public static void Move(Rigidbody2D rb, Vector2 input, float speed, float accel)
            {
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity,input.normalized*speed,accel*Time.fixedDeltaTime);
            }
            public static void Facing(Rigidbody2D rb, ref Vector2 way)
            {
                if (rb.linearVelocity == Vector2.zero) return;
                way= rb.linearVelocity.normalized;
            }
        }
    }
}
