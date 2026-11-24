using GreenPig.Shared;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GreenPig.World
{
    /// <summary>
    /// 열차 안 좌우 이동 (A/D). 대화 중에는 이동 잠금.
    /// 스프라이트 원본이 왼쪽을 보고 있으므로 오른쪽 이동 시 flipX = true.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] SpriteFrameAnimator walkAnimator;

        Rigidbody2D rb;
        float inputX;
        int lockCount;

        /// <summary>대화·연출 등에서 이동을 막을 때 사용 (중첩 가능).</summary>
        public bool IsLocked => lockCount > 0;
        public float FacingX { get; private set; } = -1f;

        public void Lock() => lockCount++;
        public void Unlock() => lockCount = Mathf.Max(0, lockCount - 1);

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (walkAnimator == null) walkAnimator = GetComponentInChildren<SpriteFrameAnimator>();
        }

        void OnEnable()
        {
            GameEvents.DialogueRequested += OnDialogueRequested;
            GameEvents.DialogueFinished += OnDialogueFinished;
        }

        void OnDisable()
        {
            GameEvents.DialogueRequested -= OnDialogueRequested;
            GameEvents.DialogueFinished -= OnDialogueFinished;
        }

        void OnDialogueRequested(string _) => Lock();
        void OnDialogueFinished(string _) => Unlock();

        void Update()
        {
            inputX = IsLocked ? 0f : ReadHorizontal();

            if (inputX != 0f)
            {
                FacingX = Mathf.Sign(inputX);
                if (spriteRenderer != null) spriteRenderer.flipX = FacingX > 0f;
            }
            if (walkAnimator != null) walkAnimator.Playing = inputX != 0f;
        }

        void FixedUpdate()
        {
            rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);
        }

        static float ReadHorizontal()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return 0f;
            float x = 0f;
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            return x;
#else
            float x = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            return x;
#endif
        }
    }
}
