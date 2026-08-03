using System.Collections;
using UnityEngine;

namespace MyCommonToolkit
{
    namespace DamageSystem {
        public class HitFlash : MonoBehaviour
        {
            [SerializeField] Health health;
            [SerializeField] Material material;
            [SerializeField, Range(0.0f, 1.0f)] float opacity;
            [SerializeField] float flashTime;
            bool isFlashing;
            void Awake()
            {
                health.OnHealthChanged += Flash;
            }
            void Flash(float damage)
            {
                if (!isFlashing && damage<0)
                    StartCoroutine(FlashEffect());
            }
            IEnumerator FlashEffect()
            {
                isFlashing = true;
                material.SetFloat("Opacity", opacity);
                yield return new WaitForSeconds(flashTime);
                material.SetFloat("Opacity", 0);
                isFlashing = false;
            }
        }
    }
}
