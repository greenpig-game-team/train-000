namespace GreenPig.Shared
{
    /// <summary>
    /// E키로 상호작용 가능한 모든 오브젝트 (승객, 창문, 연료통, 쓰레기, 열차 문 등).
    /// A: 플레이어가 범위 안의 IInteractable을 감지하고 E 입력 시 Interact() 호출.
    /// B: 승객 · 기차 업무 오브젝트에 이 interface를 구현해서 미니게임/대화를 시작.
    /// ⚠️ 공동 소유 파일: 수정 시 합의 필요.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>"E 대화하기", "E 창문 닦기" 같은 안내 문구.</summary>
        string Prompt { get; }

        /// <summary>지금 상호작용 가능한지 (이미 끝난 업무면 false).</summary>
        bool CanInteract { get; }

        void Interact();
    }
}
