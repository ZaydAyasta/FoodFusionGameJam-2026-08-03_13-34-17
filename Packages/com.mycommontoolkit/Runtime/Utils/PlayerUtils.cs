using UnityEngine;

namespace MyCommonToolkit
{
    namespace Utils2D {
        public static class PlayerUtils
        {
            public static bool isGrounded(Vector2 center, float radius, int layerMask)
                => Physics2D.OverlapCircle(center, radius, layerMask);
            public static bool isGrounded(Vector2 center, Vector2 size, int layerMask)
                => Physics2D.OverlapBox(center, size, layerMask);
            /// <summary>
            /// Call this every update
            /// </summary>
            public static bool CanCoyoteJump(float coyoteTime, bool isGrounded, ref float timer)
            {
                if (isGrounded)
                    timer = coyoteTime;
                else timer -= Time.deltaTime;
                return timer > 0;
            }
            /// <summary>
            /// Use the same timer as the one used in CanCoyoteJump();, Put all the jumping condition in an array and give them
            /// if one is false the jump will not happen. Don't forget to reset your timers if this returns true
            /// </summary>
            public static bool JumpWithCoyote(Rigidbody2D rb, float jumpForce, bool[] jumpConditions)
            {
                foreach(var jumpCondition in jumpConditions)
                    if (!jumpCondition) return false;
                rb.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);
                return true;
            }
            /// <summary>
            /// This substitutes jumpInput and isGrounded as jump conditions
            /// </summary>
            public static bool JumpBuffering(float jumpBufferingTime,bool isGrounded,bool jumpInput,ref float timer)
            {
                if(jumpInput)
                    timer = jumpBufferingTime;
                if(timer > 0)
                    timer-= Time.deltaTime;
                if(isGrounded && timer > 0)
                {
                    timer = 0;
                    return true;
                }
                return false;
            }
        }
    }
}
