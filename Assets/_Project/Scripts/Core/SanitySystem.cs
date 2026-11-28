using System;
using GreenPig.Shared;
using UnityEngine;

namespace GreenPig.Core
{
    /// <summary>
    /// 정신력(스테이지마다 리셋) + 근무 성과(엔딩용, 리셋 없이 누적) 관리.
    /// 검표 결과 · 기차 업무는 GameEvents(B → A)로 받고, 이상현상 판정은 RoundManager가 직접 호출.
    /// 정신력이 바뀌면 GameEvents.SanityChanged, 0이 되면 Depleted → GameManager가 게임오버 처리.
    /// </summary>
    public class SanitySystem : MonoBehaviour
    {
        [SerializeField] SanityConfig config;

        /// <summary>정신력 0 도달 (GameManager가 구독)</summary>
        public event Action Depleted;

        public int Current { get; private set; }
        public int Max => config != null ? config.maxSanity : 10;
        public SanityConfig Config => config;

        int ticketsChecked, ticketsCorrect, expectedTickets;
        bool depleted;

        void Awake()
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SanityConfig>();
                Debug.LogWarning("[Sanity] SanityConfig 미지정 → 기본값 사용 (Tools > GreenPig > Setup Week2 Core 실행)");
            }
        }

        void OnEnable()
        {
            GameEvents.TicketChecked += OnTicketChecked;
            GameEvents.TicketingFinished += OnTicketingFinished;
            GameEvents.TaskDone += OnTaskDone;
        }

        void OnDisable()
        {
            GameEvents.TicketChecked -= OnTicketChecked;
            GameEvents.TicketingFinished -= OnTicketingFinished;
            GameEvents.TaskDone -= OnTaskDone;
        }

        /// <summary>스테이지 시작: 정신력 10칸으로 리셋 (근무 성과는 유지)</summary>
        public void ResetForStage(int ticketCount)
        {
            depleted = false;
            ticketsChecked = ticketsCorrect = 0;
            expectedTickets = ticketCount;
            Current = Max;
            GameEvents.RaiseSanityChanged(Current, Max);
        }

        public void ApplyAnomalyResult(bool correct)
        {
            if (correct)
            {
                Change(config.anomalyCorrect, "이상현상 정답");
            }
            else
            {
                Performance.Add(config.perfAnomalyWrong, "이상현상 오답");
                Change(config.anomalyWrong, "이상현상 오답");
            }
        }

        void OnTicketChecked(bool correct)
        {
            ticketsChecked++;
            if (correct)
            {
                ticketsCorrect++;
                Change(config.ticketCorrect, "검표 정답");
            }
            else
            {
                Performance.Add(config.perfTicketWrong, "검표 오답");
                Change(config.ticketWrong, "검표 오답");
            }
        }

        void OnTicketingFinished()
        {
            if (expectedTickets > 0 && ticketsChecked >= expectedTickets && ticketsCorrect == ticketsChecked)
                Performance.Add(config.perfAllTicketsCorrect, "검표 전원 정답");
        }

        void OnTaskDone(string taskId)
        {
            Performance.Add(config.perfTaskDone, $"업무 완료({taskId})");
            Change(config.taskDone, $"업무 완료({taskId})");
        }

        void Change(int delta, string reason)
        {
            if (depleted || delta == 0) return;
            int before = Current;
            Current = Mathf.Clamp(Current + delta, 0, Max); // 이미 10칸이면 더 안 오름
            Debug.Log($"[Sanity] {reason}: {before} → {Current} / {Max}");
            if (Current == before) return;

            GameEvents.RaiseSanityChanged(Current, Max);
            if (Current == 0)
            {
                depleted = true;
                Depleted?.Invoke();
            }
        }
    }

    /// <summary>
    /// 근무 성과 점수. 스테이지가 바뀌어도 리셋되지 않고 누적 → 10주차 엔딩 등급 계산에 사용.
    /// (3주차 자동저장 때 Score를 같이 저장)
    /// </summary>
    public static class Performance
    {
        public static int Score { get; private set; }

        public static void Add(int delta, string reason)
        {
            if (delta == 0) return;
            Score += delta;
            Debug.Log($"[Performance] {reason}: {(delta > 0 ? "+" : "")}{delta} → {Score}");
        }

        /// <summary>새 게임 시작 시</summary>
        public static void ResetAll() => Score = 0;

        /// <summary>저장 데이터에서 불러올 때</summary>
        public static void Load(int score) => Score = score;
    }
}
