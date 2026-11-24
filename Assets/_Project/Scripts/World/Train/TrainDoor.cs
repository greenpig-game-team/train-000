using GreenPig.Core;
using GreenPig.Shared;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 플랫폼의 열차 문. E를 누르면 페이드 후 객실 씬으로 이동.
    /// Trigger Collider2D가 필요함.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrainDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] string targetScene = "Cabin";
        [SerializeField] string prompt = "E 탑승하기";

        bool used;

        public string Prompt => prompt;
        public bool CanInteract => !used && !SceneFader.IsTransitioning;

        public void Interact()
        {
            used = true;
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.Lock(); // 전환 중 이동 막기 (새 씬에서 새 플레이어로 초기화됨)
            SceneFader.LoadScene(targetScene);
        }

        void Reset() => GetComponent<Collider2D>().isTrigger = true;
    }
}
