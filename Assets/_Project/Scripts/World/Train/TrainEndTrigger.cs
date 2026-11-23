using System;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 열차 앞문/뒷문 판정 지점. 플레이어가 닿으면 Reached 이벤트 발생.
    /// 2주차 RoundManager가 구독해서 "앞으로 진행 / 뒤로 돌아감" 판정에 사용.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrainEndTrigger : MonoBehaviour
    {
        public enum Side { Front, Back }

        /// <summary>플레이어가 앞문(Front) 또는 뒷문(Back)에 도착</summary>
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
