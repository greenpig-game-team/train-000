using GreenPig.Shared;
using GreenPig.World;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GreenPig.Core
{
    /// <summary>
    /// 객실(Cabin) 씬의 게임 흐름. 기획 플로우차트 기준:
    /// 스테이지 시작 → 승객 탑승 · 검표(아진 파트) → 객실 순회 · 이상현상 탐색(RoundManager)
    /// → 통과 조건 달성이면 스테이지 클리어 / 정신력 소진이면 게임오버.
    /// 검표 결과 · 기차 업무는 GameEvents로 받아서 SanitySystem이 처리.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { None, Ticketing, Patrol, StageClear, GameOver }

        [Tooltip("스테이지 1~5 설정. 인덱스 0 = 1스테이지")]
        [SerializeField] StageConfig[] stages;
        [SerializeField] TrainBuilder train;
        [SerializeField] SanitySystem sanity;
        [SerializeField] RoundManager round;
        [SerializeField] PlayerController player;
        [SerializeField] FlashlightController flashlight;

        [Header("개발용")]
        [Tooltip("아진 파트 검표 미니게임이 붙기 전까지 켜두면 바로 순회 시작. 붙으면 끄기 (TicketingFinished 이벤트로 순회 시작)")]
        [SerializeField] bool skipTicketing = true;
        [Tooltip("에디터/개발 빌드에서 F2 = 검표 건너뛰기, R = (게임오버 시) 스테이지 재시작")]
        [SerializeField] bool devKeys = true;

        /// <summary>로비 · 다음 스테이지에서 씬을 다시 불러오기 전에 설정 (1부터)</summary>
        public static int RequestedStage = 1;

        public static GameManager Instance { get; private set; }
        public State Current { get; private set; } = State.None;
        public StageConfig Stage { get; private set; }

        void Awake()
        {
            Instance = this;
            if (train == null) train = FindAnyObjectByType<TrainBuilder>();
            if (sanity == null) sanity = FindAnyObjectByType<SanitySystem>();
            if (round == null) round = FindAnyObjectByType<RoundManager>();
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (flashlight == null) flashlight = FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
        }

        void OnEnable()
        {
            GameEvents.TicketingFinished += OnTicketingFinished;
            if (sanity != null) sanity.Depleted += OnSanityDepleted;
            if (round != null) round.StageCompleted += OnStageCompleted;
        }

        void OnDisable()
        {
            GameEvents.TicketingFinished -= OnTicketingFinished;
            if (sanity != null) sanity.Depleted -= OnSanityDepleted;
            if (round != null) round.StageCompleted -= OnStageCompleted;
            if (Instance == this) Instance = null;
        }

        void Start() => StartStage(RequestedStage);

        public void StartStage(int stageNumber)
        {
            Stage = FindStage(stageNumber);
            Debug.Log($"[Game] {Stage.stageNumber}스테이지 시작 — 칸 {Stage.anomalyCount}개, 손전등 {(Stage.flashlightEnabled ? "O" : "X")}");

            if (train != null) train.Build(Stage.anomalyCount);
            if (round != null) round.TeleportToSpawn();
            if (flashlight != null) flashlight.SetAvailable(Stage.flashlightEnabled);
            if (sanity != null) sanity.ResetForStage(Stage.ticketCount);

            GameEvents.RaiseStageStarted(Stage.stageNumber, Stage.anomalyCount, Stage.flashlightEnabled);

            SetState(State.Ticketing);
            if (skipTicketing) BeginPatrol();
        }

        StageConfig FindStage(int number)
        {
            if (stages != null)
                foreach (var s in stages)
                    if (s != null && s.stageNumber == number) return s;

            Debug.LogWarning($"[Game] {number}스테이지 StageConfig 없음 → 임시 설정 사용 (Tools > GreenPig > Setup Week2 Core 실행)");
            var tmp = ScriptableObject.CreateInstance<StageConfig>();
            tmp.stageNumber = number;
            tmp.anomalyCount = number switch { 1 => 5, 2 => 5, 3 => 7, _ => 8 };
            tmp.flashlightEnabled = number >= 3;
            return tmp;
        }

        void OnTicketingFinished()
        {
            if (Current == State.Ticketing) BeginPatrol();
        }

        void BeginPatrol()
        {
            if (Current != State.Ticketing) return;
            SetState(State.Patrol);
            if (round != null) round.Begin(Stage);
        }

        void OnStageCompleted()
        {
            if (Current != State.Patrol) return;
            SetState(State.StageClear);
            if (player != null) player.Lock();
            // TODO(3주차): 자동저장 (클리어한 스테이지 + Performance.Score)
            GameEvents.RaiseStageCleared(Stage.stageNumber);
        }

        void OnSanityDepleted()
        {
            if (Current == State.GameOver || Current == State.StageClear) return;
            SetState(State.GameOver);
            if (round != null) round.Stop();
            if (player != null) player.Lock();
            GameEvents.RaiseGameOver();
        }

        /// <summary>게임오버 → 같은 스테이지 처음부터 (씬 다시 불러오기)</summary>
        public void RestartStage()
        {
            RequestedStage = Stage != null ? Stage.stageNumber : 1;
            SceneFader.LoadScene(SceneManager.GetActiveScene().name);
        }

        void SetState(State s)
        {
            Debug.Log($"[Game] {Current} → {s}");
            Current = s;
        }

        void Update()
        {
            if (!devKeys || !(Application.isEditor || Debug.isDebugBuild)) return;
            if (KeyDown(DevKey.F2) && Current == State.Ticketing) BeginPatrol();
            if (KeyDown(DevKey.R) && Current == State.GameOver) RestartStage();
        }

        enum DevKey { F2, R }

        static bool KeyDown(DevKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return key == DevKey.F2 ? kb.f2Key.wasPressedThisFrame : kb.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(key == DevKey.F2 ? KeyCode.F2 : KeyCode.R);
#endif
        }
    }
}
