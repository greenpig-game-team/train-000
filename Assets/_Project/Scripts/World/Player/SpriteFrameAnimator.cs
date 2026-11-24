using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// Animator 없이 스프라이트 배열을 순서대로 넘기는 간단한 프레임 애니메이션.
    /// 정식 아트가 나오면 Animator로 교체해도 됨.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField] float fps = 12f;
        [Tooltip("멈춰 있을 때 보여줄 프레임")]
        [SerializeField] int idleFrame = 2;

        SpriteRenderer sr;
        float timer;
        int index;

        public bool Playing { get; set; }

        public void SetFrames(Sprite[] newFrames, int idle)
        {
            frames = newFrames;
            idleFrame = idle;
        }

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (frames == null || frames.Length == 0) return;

            if (!Playing)
            {
                index = idleFrame;
                timer = 0f;
                sr.sprite = frames[Mathf.Clamp(idleFrame, 0, frames.Length - 1)];
                return;
            }

            timer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, fps);
            while (timer >= step)
            {
                timer -= step;
                index = (index + 1) % frames.Length;
            }
            sr.sprite = frames[index];
        }
    }
}
