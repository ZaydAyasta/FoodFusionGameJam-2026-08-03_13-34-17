using UnityEngine;

namespace MyCommonToolkit
{
    namespace Utils2D {
        public static class KnockBack
        {
            public static Vector2 KnockBackFroce(float hitDirectionForce, Vector2 hitDirection, float constForce = 0, Vector2? constForceDirection = null, float inputForce = 0, Vector2? inputForceDirection=null)
            {
                Vector2 _hitForce = hitDirection * hitDirectionForce;
                Vector2 _constForce= constForce * (constForceDirection?? Vector2.zero);
                Vector2 _inputFroce = inputForce * (inputForceDirection ?? Vector2.zero);
                Vector2 combinedForce = _hitForce + _constForce + _inputFroce;
                return combinedForce;
            }
        }
    }
}
