using System;
using GreenPig.Shared;
using TMPro;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 열차 칸 1개. 0번 칸은 기준 칸(이상현상 없음), 1~n번 칸에 이상현상이 등장.
    /// 칸 전체를 덮는 Trigger로 플레이어가 몇 번 칸에 들어왔는지 알려줌
    /// (2주차 RoundManager 앞/뒤 판정, 3주차 칸 번호 UI에서 사용).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TrainCar : MonoBehaviour
    {
        /// <summary>플레이어가 index번 칸에 들어옴</summary>
        public static event Action<int> Entered;

        [SerializeField] int index;
        [SerializeField] SpriteRenderer background;
        [Tooltip("배경 속 칸 번호 표지판 (8번 출구식, 11/24 회의 — 기획 확정 전 placeholder). 없으면 생략")]
        [SerializeField] TMP_Text numberLabel;
        int lastIndex;
        float carWidth = 17.5f;

        public int Index => index;
        /// <summary>칸 폭 (units)</summary>
        public float Width => carWidth;
        public bool IsReference => index == 0;
        public AdBoard[] AdBoards { get; private set; }

        void Awake() => AdBoards = GetComponentsInChildren<AdBoard>(true);

        /// <summary>월드 x가 이 칸 안인지 (칸 중심 = transform.position.x)</summary>
        public bool ContainsX(float x) => Mathf.Abs(x - transform.position.x) <= carWidth * 0.5f;

        /// <summary>TrainBuilder가 생성 직후 호출</summary>
        public void Setup(int carIndex, int lastCarIndex, Sprite bg, float width)
        {
            index = carIndex;
            lastIndex = lastCarIndex;
            if (width > 0f) carWidth = width;
            gameObject.name = carIndex == 0 ? "Car_0 (기준)" : $"Car_{carIndex}";
            if (background != null && bg != null) background.sprite = bg;
            if (numberLabel != null) numberLabel.text = carIndex.ToString();

            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 3f);
            col.size = new Vector2(width, 10f);

            AdBoards = GetComponentsInChildren<AdBoard>(true);
            foreach (var ad in AdBoards)
                ad.SetId($"Car{carIndex}_{(ad.transform.localPosition.x < 0f ? "Left" : "Right")}");
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Debug.Log($"[Train] {index}번 칸 진입");
            Entered?.Invoke(index);
            GameEvents.RaiseCarEntered(index, lastIndex);
        }
    }
}
