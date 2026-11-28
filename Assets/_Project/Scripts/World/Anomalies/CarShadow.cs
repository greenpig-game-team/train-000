using System.Collections.Generic;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 승객 · 사물의 그림자 표시용 마커. 그림자 SpriteRenderer에 붙여두면
    /// ShadowlessAnomaly("그림자 없는 승객")가 칸 안에 있는 그림자를 찾아 숨김.
    /// 승객이 칸의 자식이 아니어도 x 위치로 어느 칸인지 판단하므로, 아진 파트 승객 프리팹에 붙여도 됨.
    /// </summary>
    public class CarShadow : MonoBehaviour
    {
        public static readonly List<CarShadow> All = new();

        [SerializeField] Renderer[] renderers;

        void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void SetVisible(bool visible)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;
        }
    }
}
