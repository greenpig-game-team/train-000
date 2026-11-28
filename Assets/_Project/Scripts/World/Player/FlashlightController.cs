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
        [SerializeField] float coneAngle = 80f;   // 바깥 각도 (전체) — 위아래로 넓게
        [SerializeField] float innerAngle = 50f;
        [SerializeField] float intensity = 1.5f;   // 빛 밝기 (높이면 가운데가 하얗게 날아감)
        [SerializeField, Range(0f, 1f)] float beamVisibility = 0.12f; // 허공에 보이는 빛줄기 진하기 (0이면 안 보임)
        [SerializeField] Color beamColor = new(0.75f, 0.3f, 1f); // 형광 보라
        [SerializeField] Vector2 handOffset = new(0.25f, 1.1f); // 플레이어 로컬 기준 (스케일 적용 전)

        public bool IsOn { get; private set; }

        /// <summary>이번 스테이지에서 손전등을 쓸 수 있는지 (3스테이지부터, StageConfig로 GameManager가 설정)</summary>
        public bool IsAvailable { get; private set; } = true;

        public void SetAvailable(bool available)
        {
            IsAvailable = available;
            if (!available && IsOn) SetOn(false);
        }

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
            beam.color = beamColor;
            // 배경 조명(Global Light)이 이미 1이라 Multiply 방식은 더 밝아지지 않음 → Additive(색을 더함) 방식으로 비춤
            beam.blendStyleIndex = 1;
            // 배경이 어두운 곳에서도 빛줄기 자체가 보이도록 (Volumetric)
            beam.volumetricEnabled = beamVisibility > 0f;
            beam.volumeIntensity = beamVisibility;
        }

        void Update()
        {
            if (IsAvailable && CPressed() && (player == null || !player.IsLocked)) SetOn(!IsOn);

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
