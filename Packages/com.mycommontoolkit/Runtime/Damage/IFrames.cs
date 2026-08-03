using UnityEngine;
using System.Collections;
namespace MyCommonToolkit
{
    namespace DamageSystem
    {
        public class IFrames : MonoBehaviour
        {
            [SerializeField] Material mat;
            [SerializeField] float iFrameFlashInterval;
            public bool isInvincible;
            public void StartIFrames()
            {
                if (isInvincible) return;
                isInvincible = true;
                StartCoroutine(Invincible());
            }
            public IEnumerator Invincible()
            {
                mat.SetFloat("_isFlashing", 1);
                yield return new WaitForSeconds(iFrameFlashInterval);
                mat.SetFloat("_isFlashing", 0);
                isInvincible = false;
            }
        }
    }
}
