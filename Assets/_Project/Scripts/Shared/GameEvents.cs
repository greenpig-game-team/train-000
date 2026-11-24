using System;

namespace GreenPig.Shared
{
    /// <summary>
    /// A(열차 월드 & Core)와 B(UI · 대화 · 미니게임)를 잇는 유일한 통로.
    /// 서로의 클래스를 직접 참조하지 않고, 여기 있는 event만 구독/발행한다.
    /// ⚠️ 이 파일은 공동 소유: 수정하려면 반드시 둘이 합의 후 변경.
    /// </summary>
    public static class GameEvents
    {
        // ─────────────────────────────────────────────
        // B → A  (B가 Raise, A가 구독)
        // ─────────────────────────────────────────────

        /// <summary>검표 1명 결과. 오답이면 정신력 감소 + 엔딩 영향.</summary>
        public static event Action<bool> TicketChecked;
        public static void RaiseTicketChecked(bool isCorrect) => TicketChecked?.Invoke(isCorrect);

        /// <summary>검표 미니게임 종료 → 열차 순회 시작.</summary>
        public static event Action TicketingFinished;
        public static void RaiseTicketingFinished() => TicketingFinished?.Invoke();

        /// <summary>기차 업무 완료 (예: "window", "fuel", "trash"). 정신력 +1.</summary>
        public static event Action<string> TaskDone;
        public static void RaiseTaskDone(string taskId) => TaskDone?.Invoke(taskId);

        /// <summary>대화/혼잣말 한 묶음이 끝남 → A가 다음 흐름 진행 (플레이어 이동 잠금 해제 등).</summary>
        public static event Action<string> DialogueFinished;
        public static void RaiseDialogueFinished(string dialogueId) => DialogueFinished?.Invoke(dialogueId);

        // ─────────────────────────────────────────────
        // A → B  (A가 Raise, B가 구독)
        // ─────────────────────────────────────────────

        /// <summary>정신력 변경 → 정신력 바(10칸) 갱신.</summary>
        public static event Action<int, int> SanityChanged;
        public static void RaiseSanityChanged(int current, int max) => SanityChanged?.Invoke(current, max);

        /// <summary>이번 스테이지 이상현상을 모두 찾음 → 이제 마지막 칸을 지나면 스테이지 통과 (안내 표시 등).</summary>
        public static event Action AllAnomaliesResolved;
        public static void RaiseAllAnomaliesResolved() => AllAnomaliesResolved?.Invoke();

        /// <summary>스테이지 클리어 → 결산 화면.</summary>
        public static event Action<int> StageCleared;
        public static void RaiseStageCleared(int stage) => StageCleared?.Invoke(stage);

        /// <summary>정신력 0 → 게임오버 화면.</summary>
        public static event Action GameOver;
        public static void RaiseGameOver() => GameOver?.Invoke();

        /// <summary>대화/혼잣말 시작 요청 (오프닝 독백, 튜토리얼 혼잣말 등). id는 대사 시트의 ID.</summary>
        public static event Action<string> DialogueRequested;
        public static void RaiseDialogueRequested(string dialogueId) => DialogueRequested?.Invoke(dialogueId);

        /// <summary>플레이어가 다른 칸에 들어감 → 칸 번호 UI 갱신. carIndex 0 = 기준 칸, lastIndex = 마지막 칸 번호(이상현상 수).</summary>
        public static event Action<int, int> CarEntered;
        public static void RaiseCarEntered(int carIndex, int lastIndex) => CarEntered?.Invoke(carIndex, lastIndex);

        /// <summary>손전등 켜짐/꺼짐 → 손전등 아이콘 갱신.</summary>
        public static event Action<bool> FlashlightToggled;
        public static void RaiseFlashlightToggled(bool isOn) => FlashlightToggled?.Invoke(isOn);

        /// <summary>
        /// 씬 전환 시 호출해서 남은 구독을 정리 (파괴된 오브젝트 구독으로 인한 에러 방지).
        /// 각 구독자는 OnEnable에서 +=, OnDisable에서 -= 하는 게 원칙.
        /// </summary>
        public static void ClearAll()
        {
            TicketChecked = null;
            TicketingFinished = null;
            TaskDone = null;
            DialogueFinished = null;
            SanityChanged = null;
            AllAnomaliesResolved = null;
            StageCleared = null;
            GameOver = null;
            DialogueRequested = null;
            CarEntered = null;
            FlashlightToggled = null;
        }
    }
}
