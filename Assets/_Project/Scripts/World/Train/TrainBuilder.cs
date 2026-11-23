using System.Collections.Generic;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 열차 생성기: 0번 칸(기준) + n칸을 Car 프리팹으로 이어 붙임.
    /// n = 이번 스테이지 이상현상 수 (1스테이지 5 / 2스테이지 5 / 3스테이지 7 / 4·5스테이지 8).
    /// 0번 칸이 뒤쪽(왼쪽, x=0 중심), 칸 번호가 커질수록 앞쪽(오른쪽).
    /// 마지막 칸 앞문을 지나면 TrainEndTrigger(Front) → 스테이지 통과 판정에 사용.
    /// </summary>
    public class TrainBuilder : MonoBehaviour
    {
        [SerializeField] TrainCar carPrefab;
        [Tooltip("0번 칸 뒤에 붙는 칸 수 = 이번 스테이지 이상현상 수")]
        [SerializeField, Min(1)] int anomalyCarCount = 5;
        [Tooltip("placeholder 배경. 칸 순서대로 돌려가며 재사용 (0번 칸은 항상 첫 번째)")]
        [SerializeField] Sprite[] backgrounds;
        [SerializeField] bool buildOnAwake = true;

        readonly List<TrainCar> cars = new();

        public IReadOnlyList<TrainCar> Cars => cars;
        public int AnomalyCarCount => anomalyCarCount;
        public float CarWidth { get; private set; }
        /// <summary>열차 뒤 끝(0번 칸 뒷벽) x</summary>
        public float MinX { get; private set; }
        /// <summary>열차 앞 끝(마지막 칸 앞벽) x</summary>
        public float MaxX { get; private set; }

        void Awake()
        {
            if (buildOnAwake) Build(anomalyCarCount);
        }

        /// <summary>스테이지 시작 시 GameManager가 이상현상 수를 넣어 다시 만들 수 있음.</summary>
        public void Build(int count)
        {
            Clear();
            anomalyCarCount = Mathf.Max(1, count);

            var firstBg = backgrounds != null && backgrounds.Length > 0 ? backgrounds[0] : null;
            CarWidth = firstBg != null ? firstBg.bounds.size.x : 17.5f;
            MinX = -CarWidth * 0.5f;
            MaxX = MinX + CarWidth * (anomalyCarCount + 1);

            for (int i = 0; i <= anomalyCarCount; i++)
            {
                var car = Instantiate(carPrefab, transform);
                car.transform.localPosition = new Vector3(CarWidth * i, 0f, 0f);
                car.Setup(i, anomalyCarCount, PickBackground(i), CarWidth);
                cars.Add(car);
            }

            EndTrigger("Door_Back (0번 칸 뒷문)", TrainEndTrigger.Side.Back, MinX + 1f);
            EndTrigger($"Door_Front ({anomalyCarCount}번 칸 앞문)", TrainEndTrigger.Side.Front, MaxX - 1f);
            Wall("Wall_Back", MinX - 0.5f);
            Wall("Wall_Front", MaxX + 0.5f);

            var follow = FindAnyObjectByType<CameraFollow2D>();
            if (follow != null) follow.SetBounds(MinX, MaxX);
        }

        Sprite PickBackground(int i)
        {
            if (backgrounds == null || backgrounds.Length == 0) return null;
            return backgrounds[i % backgrounds.Length];
        }

        void Clear()
        {
            cars.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        void EndTrigger(string name, TrainEndTrigger.Side side, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, 3f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 10f);
            go.AddComponent<TrainEndTrigger>().SetSide(side);
        }

        void Wall(string name, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, 3f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 10f);
        }
    }
}
