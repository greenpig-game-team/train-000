using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 열차 광고판. 배경과 분리된 오브젝트라서 이상현상(광고판 변경)에서 스프라이트만 바꿔치기 가능.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AdBoard : MonoBehaviour
    {
        [SerializeField] string boardId;
        SpriteRenderer sr;
        Sprite original;
        Color originalColor;
        bool originalFlipX;

        public string BoardId => boardId;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            original = sr.sprite;
            originalColor = sr.color;
            originalFlipX = sr.flipX;
        }

        public void SetId(string id) => boardId = id;

        /// <summary>이상현상: 다른 광고로 교체</summary>
        public void Swap(Sprite anomaly) => sr.sprite = anomaly;

        /// <summary>바뀐 광고 아트가 없을 때 쓰는 placeholder 이상현상 (좌우반전 + 색 변경)</summary>
        public void SetPlaceholderAnomaly(bool on, Color tint)
        {
            sr.flipX = on ? !originalFlipX : originalFlipX;
            sr.color = on ? tint : originalColor;
        }

        /// <summary>정상 상태로 복구</summary>
        public void Restore()
        {
            sr.sprite = original;
            sr.color = originalColor;
            sr.flipX = originalFlipX;
        }
    }
}
