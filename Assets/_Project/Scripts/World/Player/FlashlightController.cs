using GreenPig.Shared;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GreenPig.World
{
    /// <summary>
    /// 손전등 (C키 토글). 플레이어가 보는 방향으로 Light2D cone을 비추고,
    /// 빛 범위(반경 + 각도) 안에 있는 FlashlightTarget에 비춤 여부를 알려줌.
    /// </summary>
    public class FlashlightController : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] Light2D beam;

        [Header("빛 모양")]
        [SerializeField] float range = 7f;
        [SerializeField] float coneAngle = 50f;   // 바깥 각도 (전체)
        [SerializeField] float innerAngle = 25f;
        [SerializeField] float intensity = 0.6f;   // 밝은 배경 위에 은은하게 보이는 정도
        [SerializeField] Vector2 handOffset = new(0.25f, 1.1f); // 플레이어 로컬 기준 (스케일 적용 전)

        public bool IsOn { get; private set; }

        void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (beam == null) beam = GetComponentInChildren<Light2D>(true);
            // 플레이어 스케일(2.4배)이 빛 반경에 곱해지지 않도록 상쇄
            float s = transform.lossyScale.x;
            if (s > 0f) beam.transform.localScale = Vector3.one / s;
            ApplyShape();
            SetOn(false, notify: false);
        }

        void OnValidate()
        {
            if (beam != null) ApplyShape();
        }

        void ApplyShape()
        {
            beam.lightType = Light2D.LightType.Point;
            beam.pointLightOuterRadius = range;
            beam.pointLightInnerRadius = 0.5f;
            beam.pointLightOuterAngle = coneAngle;
            beam.pointLightInnerAngle = innerAngle;
            beam.intensity = intensity;
            beam.color = new Color(1f, 0.95f, 0.8f);
        }

        void Update()
        {
            if (CPressed() && (player == null || !player.IsLocked)) SetOn(!IsOn);

            // 플레이어가 보는 방향으로 빛 회전 (Light2D는 transform.up 방향을 비춤)
            float facing = player != null ? player.FacingX : 1f;
            beam.transform.localPosition = new Vector3(handOffset.x * facing, handOffset.y, 0f);
            beam.transform.rotation = Quaternion.Euler(0f, 0f, facing > 0f ? -90f : 90f);

            UpdateTargets();
        }

        public void SetOn(bool on, bool notify = true)
        {
            IsOn = on;
            beam.enabled = on;
            if (notify) GameEvents.RaiseFlashlightToggled(on);
        }

        void UpdateTargets()
        {
            Vector2 origin = beam.transform.position;
            Vector2 dir = beam.transform.up;
            float half = coneAngle * 0.5f;

            foreach (var t in FlashlightTarget.All)
            {
                bool lit = false;
                if (IsOn)
                {
                    Vector2 to = (Vector2)t.transform.position - origin;
                    lit = to.magnitude <= range && Vector2.Angle(dir, to) <= half;
                }
                t.SetLit(lit);
            }
        }

        static bool CPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }
    }
}
