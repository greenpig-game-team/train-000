using System.Collections.Generic;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 열차 생성기: 0번 칸(기준) + n칸을 Car 프리팹으로 이어 붙임.
    /// n = 이번 스테이지 이상현상 수 (1스테이지 5 / 2스테이지 5 / 3스테이지 7 / 4·5스테이지 8).
    /// 0번 칸이 왼쪽(x=0 중심), 칸 번호가 커질수록 오른쪽. 칸 사이에는 문이 없고 일자로 이어짐.
    /// 문은 열차 전체에 하나씩 (11/27 확인):
    ///  - 앞문(Return): 0번 칸 왼쪽 끝. 이상현상을 발견하면 여기까지 돌아옴
    ///  - 뒷문(Exit): 마지막 칸 오른쪽 끝. 이상현상을 다 찾고 여기로 나가면 스테이지 완료
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
        /// <summary>열차 왼쪽 끝(0번 칸, 앞문 쪽) x</summary>
        public float MinX { get; private set; }
        /// <summary>열차 오른쪽 끝(마지막 칸, 뒷문 쪽) x</summary>
        public float MaxX { get; private set; }
        /// <summary>플레이어 출발 위치 x (0번 칸, 앞문 바로 옆)</summary>
        public float SpawnX => MinX + 3f;

        /// <summary>index번 칸의 왼쪽 경계 x</summary>
        public float CarStartX(int index) => MinX + CarWidth * index;

        /// <summary>월드 x → 칸 번호 (열차 밖이면 0 또는 마지막 칸으로 clamp)</summary>
        public int CarIndexAt(float x) =>
            Mathf.Clamp(Mathf.FloorToInt((x - MinX) / Mathf.Max(0.01f, CarWidth)), 0, anomalyCarCount);

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

            EndTrigger("Door_Return (앞문, 0번 칸)", TrainEndTrigger.Side.Return, MinX + 1f);
            EndTrigger($"Door_Exit (뒷문, {anomalyCarCount}번 칸)", TrainEndTrigger.Side.Exit, MaxX - 1f);
            Wall("Wall_Left", MinX - 0.5f);
            Wall("Wall_Right", MaxX + 0.5f);

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
                // Destroy는 프레임 끝에 처리되므로 먼저 꺼서 CarShadow.All 등 등록 목록에서 바로 빠지게 함
                child.SetActive(false);
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
