using System.Collections.Generic;
using GreenPig.Shared;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GreenPig.World
{
    /// <summary>
    /// 플레이어 주변의 IInteractable(Trigger Collider2D)을 감지하고 E키로 Interact() 호출.
    /// 범위 안에 여러 개면 가장 가까운 것을 선택.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        readonly List<Component> nearby = new();
        PlayerController controller;

        /// <summary>지금 E를 누르면 상호작용할 대상 (없으면 null). B가 "E 대화하기" 프롬프트 UI에 사용 가능.</summary>
        public IInteractable Current { get; private set; }

        void Awake() => controller = GetComponent<PlayerController>();

        void OnTriggerEnter2D(Collider2D other)
        {
            var it = other.GetComponentInParent<IInteractable>() as Component;
            if (it != null && !nearby.Contains(it)) nearby.Add(it);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var it = other.GetComponentInParent<IInteractable>() as Component;
            if (it != null) nearby.Remove(it);
        }

        void Update()
        {
            Current = FindClosest();

            if (Current == null || controller.IsLocked) return;
            if (EPressed() && Current.CanInteract) Current.Interact();
        }

        IInteractable FindClosest()
        {
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = nearby.Count - 1; i >= 0; i--)
            {
                var c = nearby[i];
                if (c == null) { nearby.RemoveAt(i); continue; }
                var it = (IInteractable)c;
                if (!it.CanInteract) continue;
                float d = Mathf.Abs(c.transform.position.x - transform.position.x);
                if (d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }

        static bool EPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }
    }
}
