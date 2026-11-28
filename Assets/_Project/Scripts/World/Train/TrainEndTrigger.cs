using System;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 열차 양 끝의 문 (TrainBuilder가 생성). 문은 지하철 전체에 하나씩만 있음 (11/27 확인).
    /// Return = 앞문: 0번(기준) 칸 쪽 끝. 이상현상을 발견하면 여기까지 돌아오면 "있음" 판정.
    /// Exit   = 뒷문: 마지막(n번) 칸 끝. 이상현상을 다 찾은 뒤 여기로 나가면 스테이지 완료.
    /// 판정은 RoundManager가 Reached 이벤트를 받아서 처리.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrainEndTrigger : MonoBehaviour
    {
        public enum Side { Return, Exit }

        /// <summary>플레이어가 앞문(Return) 또는 뒷문(Exit)에 도착</summary>
        public static event Action<Side> Reached;

        [SerializeField] Side side = Side.Exit;

        public Side DoorSide => side;
        public void SetSide(Side s) => side = s;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Debug.Log($"[Train] {(side == Side.Return ? "앞문(돌아가기)" : "뒷문(나가기)")} 도착");
            Reached?.Invoke(side);
        }
    }
}
