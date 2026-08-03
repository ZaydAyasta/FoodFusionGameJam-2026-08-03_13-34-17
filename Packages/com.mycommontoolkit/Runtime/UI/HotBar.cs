using System;
using UnityEngine;
namespace MyCommonToolkit
{
    namespace UIelements {
        /// <summary>
        /// Create a hotbar object. You can pair this class with the Scroller.
        /// </summary>
        public class HotBar
        {
            public int slotCount { get; private set; }
            public GameObject[] slots { get; private set; }
            public static event Action<int> OnIndexChanged;
            int index;
            public HotBar(int slotCount,GameObject slotPre, MonoBehaviour hotbar)
            {
                this.slotCount= slotCount;
                slots = new GameObject[slotCount];
                for (int i = 0; i < slotCount; i++)
                {
                    slots[i] = UnityEngine.Object.Instantiate(slotPre, hotbar.transform);
                }
            }
            public void SetIndex(int i)
            {
                if (i < 0 || i > slotCount) return;
                if (i == index) return;
                index = i;
                OnIndexChanged?.Invoke(index);
            }
            public void Next() => SetIndex((index + 1) % slotCount);

            public void Previous() => SetIndex((index - 1 + slotCount) % slotCount);
        }
    }
}
