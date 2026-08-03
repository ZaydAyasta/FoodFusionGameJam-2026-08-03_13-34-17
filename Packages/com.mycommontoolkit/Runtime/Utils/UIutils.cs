using UnityEngine;

namespace MyCommonToolkit
{
    public static class UIutils
    {
        public static float SmoothFill(float current, float target, float speed, float deltaTime)
            => Mathf.Lerp(current, target,1-Mathf.Exp(-speed * deltaTime));
        public static bool IsPointerOverUI()
            => UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        public static Vector2 WorldToUI(Camera cam, RectTransform canvas, Vector3 worldPos)
        {
            Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screenPos,cam,out Vector2 localPos);
            return localPos;
        }
        public static Vector2 ClampToScreen(Vector2 pos, Vector2 screenSize, float padding = 50f)
        {
            pos.x = Mathf.Clamp(pos.x, padding, screenSize.x - padding);
            pos.y = Mathf.Clamp(pos.y, padding, screenSize.y - padding);
            return pos;
        }
        /// <summary>
        /// ATTENTION:THIS FUNCTION REQUIRES A "CoroutineRunner" IN THE SCENE!
        /// </summary>
        public static void Shake(Camera cam, float intensity, int times = 1, float interval = 0.1f)
        {
            CoroutineRunner.Instance.StartCoroutine(ShakeEffect(cam, intensity, times, interval));
        }
        static System.Collections.IEnumerator ShakeEffect(Camera cam, float intensity, int times, float interval)
        {
            Vector3 original = cam.transform.position;

            for (int i = 0; i < times; i++)
            {
                cam.transform.position = original + (Vector3)(Random.insideUnitCircle * intensity);
                yield return new WaitForSeconds(interval);
                cam.transform.position = original;
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
