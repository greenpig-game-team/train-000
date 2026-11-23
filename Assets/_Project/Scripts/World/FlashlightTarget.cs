using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GreenPig.World
{
    /// <summary>
    /// 손전등에 비춰졌는지 판정받는 오브젝트 (손전등형 이상현상: 쳐다보는 승객들, 사고 흔적 등).
    /// revealOnlyWhenLit을 켜면 빛이 닿을 때만 보임.
    /// </summary>
    public class FlashlightTarget : MonoBehaviour
    {
        public static readonly List<FlashlightTarget> All = new();

        [SerializeField] bool revealOnlyWhenLit = false;
        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] float fadeSpeed = 6f;

        public UnityEvent onLit;
        public UnityEvent onUnlit;

        public bool IsLit { get; private set; }
        float alpha;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();
            alpha = revealOnlyWhenLit ? 0f : 1f;
            ApplyAlpha();
        }

        public void SetRevealOnly(bool on) => revealOnlyWhenLit = on;

        public void SetLit(bool lit)
        {
            if (lit == IsLit) return;
            IsLit = lit;
            if (lit) onLit?.Invoke(); else onUnlit?.Invoke();
        }

        void Update()
        {
            if (!revealOnlyWhenLit) return;
            alpha = Mathf.MoveTowards(alpha, IsLit ? 1f : 0f, fadeSpeed * Time.deltaTime);
            ApplyAlpha();
        }

        void ApplyAlpha()
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var c = r.color;
                c.a = alpha;
                r.color = c;
            }
        }
    }
}
