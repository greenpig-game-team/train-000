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

        public string BoardId => boardId;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            original = sr.sprite;
        }

        public void SetId(string id) => boardId = id;

        /// <summary>이상현상: 다른 광고로 교체</summary>
        public void Swap(Sprite anomaly) => sr.sprite = anomaly;

        /// <summary>정상 상태로 복구</summary>
        public void Restore() => sr.sprite = original;
    }
}
