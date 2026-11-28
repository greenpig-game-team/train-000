using UnityEngine;

namespace GreenPig.Core
{
    /// <summary>
    /// 정신력 · 근무 성과 수치. 코드에 고정하지 않고 에셋으로 분리 → 기획팀이 플레이테스트 후 인스펙터에서 조정.
    /// 기본값은 11/23 기획팀 확인 기준:
    /// 검표 오답 −1 / 이상현상 오답 −2 / 검표 정답 +1 / 기차 업무 +1 / 이상현상 정답은 회복 없음 / 최대 10칸.
    /// </summary>
    [CreateAssetMenu(menuName = "GreenPig/Sanity Config", fileName = "SanityConfig")]
    public class SanityConfig : ScriptableObject
    {
        [Header("정신력 (칸)")]
        [Min(1)] public int maxSanity = 10;
        [Tooltip("검표 오답")] public int ticketWrong = -1;
        [Tooltip("열차 내 이상현상 오답 (0번 칸 복귀 후 다음 칸 진입 시 적용)")] public int anomalyWrong = -2;
        [Tooltip("검표 정답")] public int ticketCorrect = 1;
        [Tooltip("이상현상 정답 (기획팀 확인: 회복 없음)")] public int anomalyCorrect = 0;
        [Tooltip("기차 업무 완료")] public int taskDone = 1;

        [Header("근무 성과 (엔딩, 스테이지 간 누적)")]
        public int perfTicketWrong = -1;
        [Tooltip("이상현상이 메인이라 검표보다 크게")] public int perfAnomalyWrong = -2;
        public int perfTaskDone = 1;
        [Tooltip("한 스테이지 검표 전원 정답 보너스")] public int perfAllTicketsCorrect = 2;
    }
}
