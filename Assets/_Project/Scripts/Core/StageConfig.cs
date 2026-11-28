using UnityEngine;

namespace GreenPig.Core
{
    /// <summary>
    /// 스테이지별 설정. 이상현상 수 = 0번 칸 뒤에 붙는 칸 수.
    /// (1스테이지 5 / 2스테이지 5 / 3스테이지 7 / 4스테이지 8 / 5스테이지 8, 손전등은 3스테이지부터 — 11/24 확정)
    /// </summary>
    [CreateAssetMenu(menuName = "GreenPig/Stage Config", fileName = "Stage1")]
    public class StageConfig : ScriptableObject
    {
        [Min(1)] public int stageNumber = 1;

        [Tooltip("이번 스테이지 이상현상 수 = 1번~n번 칸 수")]
        [Min(1)] public int anomalyCount = 5;

        [Tooltip("손전등(C) 사용 가능 여부. 3스테이지부터 true")]
        public bool flashlightEnabled = false;

        [Tooltip("검표할 승객 수 (전원 정답 보너스 판정용)")]
        [Min(0)] public int ticketCount = 3;

        [Tooltip("이번 스테이지에 나올 수 있는 이상현상 id (AnomalyBase.Id). 비워두면 씬에 있는 이상현상 전부 사용")]
        public string[] anomalyIds = new string[0];
    }
}
