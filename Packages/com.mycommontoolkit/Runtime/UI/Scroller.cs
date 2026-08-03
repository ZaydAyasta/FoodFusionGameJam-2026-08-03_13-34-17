using UnityEngine;
namespace MyCommonToolkit
{
    namespace UIelements
    {
        /// <summary>
        /// Paired with the hotbar.
        /// </summary>
        public class Scroller
        {
            float speed;
            AnimationCurve speedCurve;
            int index;
            int targetIndex;
            RectTransform[] slots;
            float t;

            MonoBehaviour context;
            public Scroller(HotBar hotbar, float speed, AnimationCurve speedCurve, MonoBehaviour context)
            {
                this.speed = speed;
                this.speedCurve = speedCurve;
                this.context= context;
                slots = new RectTransform[hotbar.slotCount];
                for (int i = 0; i < hotbar.slotCount; i++)
                {
                    slots[i] = hotbar.slots[i].transform as RectTransform;
                }
                Canvas.ForceUpdateCanvases();
                context.transform.position = slots[0].position;
                hotbar.SetIndex(6);
                HotBar.OnIndexChanged += ChangeSlot;
            }
            void ChangeSlot(int index)
            {
                targetIndex = index;
            }
            public void ScrollerUpdate()
            {
                if (targetIndex != index)
                {
                    context.transform.position = Utils2D.MoveUtils.MoveLerp(context.transform.position, slots[targetIndex].position, ref t, speed, speedCurve);
                    if (Vector3.Distance(context.transform.position, slots[targetIndex].position) < 0.005f)
                        targetIndex = index;
                }
            }
        }
    }
}
