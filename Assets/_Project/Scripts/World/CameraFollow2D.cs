using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 플레이어를 X축으로만 부드럽게 따라가는 카메라.
    /// useBounds를 켜면 열차 칸 양 끝(minX~maxX) 밖으로 카메라가 나가지 않음.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.15f;
        [SerializeField] float offsetX = 0f;

        [Header("카메라 경계 (열차 칸 끝)")]
        [SerializeField] bool useBounds = false;
        [SerializeField] float minX = -10f;
        [SerializeField] float maxX = 10f;

        Camera cam;
        float velocityX;

        public void SetTarget(Transform t) => target = t;

        public void SetBounds(float min, float max)
        {
            useBounds = true;
            minX = min;
            maxX = max;
        }

        void Awake() => cam = GetComponent<Camera>();

        /// <summary>부드럽게 따라가지 않고 바로 플레이어 위치로 (순간이동 직후 사용)</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            velocityX = 0f;
            var p = transform.position;
            p.x = Goal();
            transform.position = p;
        }

        float Goal()
        {
            float goal = target.position.x + offsetX;
            if (useBounds && cam != null && cam.orthographic)
            {
                float halfWidth = cam.orthographicSize * cam.aspect;
                float lo = minX + halfWidth, hi = maxX - halfWidth;
                goal = lo > hi ? (minX + maxX) * 0.5f : Mathf.Clamp(goal, lo, hi);
            }
            return goal;
        }

        void LateUpdate()
        {
            if (target == null) return;

            float goal = Goal();

            var p = transform.position;
            p.x = Mathf.SmoothDamp(p.x, goal, ref velocityX, smoothTime);
            transform.position = p;
        }
    }
}
