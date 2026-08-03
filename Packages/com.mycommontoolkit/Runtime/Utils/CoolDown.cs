using System.Collections.Generic;
using UnityEngine;

namespace MyCommonToolkit
{
    public static class Cooldown
    {
        private static Dictionary<string, float> cooldowns = new();

        public static bool IsReadyToTrigger(string key)
        {
            return !cooldowns.TryGetValue(key, out float time) || Time.time >= time;
        }
        public static string GetUniqueCooldownKey(string key)
        {
            string newKey = key;
            int counter = 0;
            while (cooldowns.ContainsKey(newKey))
            {
                counter++;
                newKey = key + counter;
            }
            return newKey;
        }
        public static void Trigger(string key, float duration)
        {
            cooldowns[key] = Time.time + duration;
        }

        public static float Remaining(string key)
        {
            if (!cooldowns.TryGetValue(key, out float time))
                return 0f;

            return Mathf.Max(0f, time - Time.time);
        }

        public static void Remove(string key)
        {
            cooldowns.Remove(key);
        }
        public static void Clear()
        {
            foreach (var cooldown in cooldowns.Keys)
                Remove(cooldown);
        }
    }
}
