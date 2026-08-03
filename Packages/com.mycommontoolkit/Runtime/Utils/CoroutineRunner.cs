using UnityEngine;

namespace MyCommonToolkit
{
    public class CoroutineRunner : MonoBehaviour
    {
        public static CoroutineRunner Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            GameObject obj = new GameObject("CoroutineRunner");
            Instance = obj.AddComponent<CoroutineRunner>();
            DontDestroyOnLoad(obj);
        }
    }
}
