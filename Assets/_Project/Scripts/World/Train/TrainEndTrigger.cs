using System;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 열차 양 끝 판정 지점 (TrainBuilder가 생성). 플레이어가 닿으면 Reached 이벤트 발생.
    /// Front = 마지막(n번) 칸 앞문 → 이상현상을 다 찾았으면 스테이지 통과,
    /// Back = 0번(기준) 칸 뒷문. 칸 단위 위치는 TrainCar.Entered로 받음.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrainEndTrigger : MonoBehaviour
    {
        public enum Side { Front, Back }

        /// <summary>플레이어가 마지막 칸 앞문(Front) 또는 0번 칸 뒷문(Back)에 도착</summary>
        public static event Action<Side> Reached;

        [SerializeField] Side side = Side.Front;

        public void SetSide(Side s) => side = s;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Debug.Log($"[Train] {side} 도착");
            Reached?.Invoke(side);
        }
    }
}
