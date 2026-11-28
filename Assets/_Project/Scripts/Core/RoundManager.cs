using System;
using System.Collections.Generic;
using System.Linq;
using GreenPig.Shared;
using GreenPig.World;
using UnityEngine;

namespace GreenPig.Core
{
    /// <summary>
    /// 열차 순회 판정 (8번 출구 방식). 기준: 스크립트(스테이지 1 튜토리얼) + 11/27 문 구조 확인.
    ///
    /// - 열차 = 0번 칸(기준, 이상현상 없음) + 1~n번 칸. n = 이상현상 수. 칸 사이에 문 없음, 일자로 이어짐.
    /// - 아직 못 찾은 칸 중 가장 앞 칸(target)에 이상현상이 하나 켜져 있음. 이미 찾은 칸은 정상.
    /// - 정답: target 칸에 들어가서 이상현상을 봤고 → 뒤돌아 앞문(0번 칸 끝)까지 돌아옴
    ///         → 그 이상현상 해결, 다음 칸에 새 이상현상
    /// - 오답 ① 있는데 지나침: target 칸을 지나 다음 칸으로 넘어감 → 페이드 후 0번 칸으로 되돌림
    ///   오답 ② 없는데 뒤로: target 칸에 가기 전에(또는 다 찾은 뒤에) 앞문으로 돌아옴
    ///   → 둘 다 정신력 −2는 0번 칸 복귀 후 "다음 칸 진입 즉시" 적용, 틀린 이상현상은 그대로 다시 등장
    /// - 이상현상을 다 찾은 뒤 뒷문(마지막 칸 끝)으로 나가면 StageCompleted
    ///
    /// 칸 판정은 Trigger 대신 플레이어 x 위치로 계산 (칸 경계에서 왔다 갔다 해도 오판정 안 나도록 여유 margin).
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        [SerializeField] TrainBuilder train;
        [SerializeField] SanitySystem sanity;
        [SerializeField] PlayerController player;
        [Tooltip("이상현상 후보들 (씬의 Anomalies 오브젝트). 비우면 씬에서 자동 수집")]
        [SerializeField] List<AnomalyBase> anomalies = new();

        [Header("판정 여유 (units)")]
        [Tooltip("target 칸에 이만큼 들어가야 '이상현상을 봤다'로 인정")]
        [SerializeField] float seeMargin = 1.5f;
        [Tooltip("다음 칸으로 이만큼 넘어가야 '지나쳤다'로 판정 (경계에서 실수로 한 발 넘어간 건 봐줌)")]
        [SerializeField] float passMargin = 1.0f;

        /// <summary>이상현상을 다 찾고 뒷문으로 나감 → GameManager가 스테이지 클리어 처리</summary>
        public event Action StageCompleted;

        /// <summary>판정 결과 (디버그 · 연출용, A 내부)</summary>
        public event Action<bool> Judged;

        readonly List<AnomalyBase> pool = new();
        readonly Dictionary<AnomalyBase, int> useCount = new();

        bool running, resetting, pendingPenalty;
        bool leftCar0;      // 이번 회차에 1번 칸 이상으로 나갔는지 (출발 직후 앞문에 닿는 건 무시)
        bool seenTarget;    // 이번 회차에 target 칸 안까지 들어갔는지
        int target;         // 이상현상이 켜진 칸 번호. n+1이면 전부 찾음
        AnomalyBase active;

        public int CarCount => train != null ? train.AnomalyCarCount : 0;
        public int FoundCount { get; private set; }
        public int TargetCar => target;
        public bool AllFound => target > CarCount;

        void Awake()
        {
            if (train == null) train = FindAnyObjectByType<TrainBuilder>();
            if (sanity == null) sanity = FindAnyObjectByType<SanitySystem>();
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (anomalies.Count == 0) anomalies.AddRange(FindObjectsByType<AnomalyBase>(FindObjectsSortMode.None));
        }

        void OnEnable() => TrainEndTrigger.Reached += OnDoorReached;
        void OnDisable() => TrainEndTrigger.Reached -= OnDoorReached;

        /// <summary>순회 시작 (검표가 끝난 뒤 GameManager가 호출). 열차는 이미 Build된 상태여야 함.</summary>
        public void Begin(StageConfig stage)
        {
            Stop();
            pool.Clear();
            useCount.Clear();
            var ids = stage != null ? stage.anomalyIds : null;
            foreach (var a in anomalies)
            {
                if (a == null) continue;
                if (ids != null && ids.Length > 0 && !ids.Contains(a.Id)) continue;
                pool.Add(a);
                useCount[a] = 0;
            }
            if (pool.Count == 0) Debug.LogWarning("[Round] 사용할 이상현상이 없음 (Anomalies 오브젝트 / StageConfig.anomalyIds 확인)");

            FoundCount = 0;
            target = 1;
            pendingPenalty = false;
            ResetRun();
            running = true;
            AssignTarget();
            Debug.Log($"[Round] 순회 시작 — 0번 칸 + {CarCount}칸, 이상현상 {CarCount}개");
        }

        public void Stop()
        {
            running = false;
            if (active != null) active.Deactivate();
            active = null;
        }

        void Update()
        {
            if (!running || resetting || player == null || train == null) return;

            float x = player.transform.position.x;
            int car = train.CarIndexAt(x);

            if (car >= 1)
            {
                leftCar0 = true;
                if (pendingPenalty)
                {
                    // 오답 페널티는 0번 칸 복귀 후 다음 칸에 들어서는 순간 적용 (스크립트 기준)
                    pendingPenalty = false;
                    sanity?.ApplyAnomalyResult(false);
                }
            }

            if (AllFound) return;

            if (x > train.CarStartX(target) + seeMargin) seenTarget = true;

            // 이상현상이 있는 칸을 그대로 지나침
            if (x > train.CarStartX(target + 1) + passMargin)
                Wrong($"{target}번 칸 이상현상을 지나침", sendBack: true);
        }

        void OnDoorReached(TrainEndTrigger.Side side)
        {
            if (!running || resetting) return;

            if (side == TrainEndTrigger.Side.Return)
            {
                if (!leftCar0) return; // 출발 직후 앞문에 닿은 건 판정 안 함

                if (!AllFound && seenTarget) Correct();
                else Wrong(AllFound ? "이상현상을 다 찾았는데 뒤로 돌아감" : "이상현상이 없는데 뒤로 돌아감", sendBack: false);
            }
            else // Exit = 뒷문
            {
                if (AllFound)
                {
                    running = false;
                    Debug.Log("[Round] 이상현상 전부 발견 + 뒷문 통과 → 스테이지 완료");
                    StageCompleted?.Invoke();
                }
                else
                {
                    Wrong("이상현상을 남긴 채 뒷문 도착", sendBack: true);
                }
            }
        }

        void Correct()
        {
            Debug.Log($"[Round] 정답: {target}번 칸 이상현상({(active != null ? active.Id : "-")}) 발견");
            if (active != null) active.Deactivate();
            active = null;

            FoundCount++;
            target++;
            sanity?.ApplyAnomalyResult(true);
            Judged?.Invoke(true);
            ResetRun();

            if (AllFound)
            {
                Debug.Log("[Round] 이상현상 전부 발견 → 마지막 칸 뒷문으로 나가면 스테이지 완료");
                GameEvents.RaiseAllAnomaliesResolved();
            }
            else
            {
                AssignTarget();
            }
        }

        void Wrong(string reason, bool sendBack)
        {
            Debug.Log($"[Round] 오답: {reason} → 0번 칸, 다음 칸 진입 시 정신력 감소");
            pendingPenalty = true;
            Judged?.Invoke(false);
            ResetRun();
            // 틀린 이상현상은 같은 칸에 그대로 다시 등장 (active 유지)

            if (sendBack) SendBackToCar0();
        }

        void ResetRun()
        {
            leftCar0 = false;
            seenTarget = false;
        }

        void SendBackToCar0()
        {
            if (SceneFader.IsTransitioning)
            {
                TeleportToSpawn(); // 이미 다른 페이드 중이면 바로 이동
                return;
            }
            resetting = true;
            player.Lock();
            SceneFader.FadeOutIn(
                onBlack: TeleportToSpawn,
                onDone: () =>
                {
                    player.Unlock();
                    resetting = false;
                });
        }

        /// <summary>플레이어를 0번 칸 출발 위치로 (GameManager도 스테이지 시작 때 사용)</summary>
        public void TeleportToSpawn()
        {
            if (player == null || train == null) return;
            var pos = new Vector3(train.SpawnX, player.transform.position.y, player.transform.position.z);
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = pos;
            }
            player.transform.position = pos;
            var cam = FindAnyObjectByType<CameraFollow2D>();
            if (cam != null) cam.SnapToTarget();
        }

        void AssignTarget()
        {
            while (target <= CarCount)
            {
                var car = train.Cars.Count > target ? train.Cars[target] : null;
                var pick = Pick(car);
                if (pick != null)
                {
                    active = pick;
                    useCount[pick]++;
                    pick.Activate(car);
                    return;
                }
                // 이 칸에 맞는 이상현상이 없음 → 정상 칸으로 두고 다음 칸으로 (경고만)
                Debug.LogWarning($"[Round] {target}번 칸에 배정할 수 있는 이상현상이 없음 → 이 칸은 건너뜀");
                FoundCount++;
                target++;
            }
            GameEvents.RaiseAllAnomaliesResolved();
        }

        /// <summary>덜 나온 이상현상부터 골고루 (같은 횟수면 랜덤)</summary>
        AnomalyBase Pick(TrainCar car)
        {
            if (car == null) return null;
            var candidates = pool.Where(a => a.CanApplyTo(car)).ToList();
            if (candidates.Count == 0) return null;
            int min = candidates.Min(a => useCount[a]);
            var least = candidates.Where(a => useCount[a] == min).ToList();
            return least[UnityEngine.Random.Range(0, least.Count)];
        }
    }
}
