#if UNITY_EDITOR
using System.IO;
using GreenPig.Core;
using GreenPig.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GreenPig.EditorTools
{
    /// <summary>
    /// Tools > GreenPig > Setup Week2 Core (판정 · 정신력 · 이상현상)
    /// 1) Data/Config에 SanityConfig + Stage1~5 에셋 생성 (이미 있으면 그대로 둠 → 기획팀 수정값 보존)
    /// 2) TrainCar 프리팹에 칸 번호 표지판(placeholder) + 그림자 있는 임시 승객 2명(그림자 X 이상현상 테스트용) 추가
    /// 3) Cabin 씬에 [Game] (GameManager · SanitySystem · RoundManager) + Anomalies(광고판 교체 · 그림자 없는 승객) 배치
    /// 여러 번 실행해도 됨 (같은 이름 오브젝트는 지우고 다시 만듦).
    /// </summary>
    public static class Week2SetupTool
    {
        const string ConfigDir = "Assets/_Project/Data/Config";
        const string CarPrefabPath = "Assets/_Project/Prefabs/World/TrainCar.prefab";
        const string CabinScene = "Assets/_Project/Scenes/Cabin.unity";
        const string WhitePath = "Assets/_Project/Art/Sprites/Placeholder/White.png";

        // 기획 확정값 (변칙성/이상현상 페이지 수 정리, 손전등 3스테이지부터 11/24)
        static readonly int[] AnomalyCounts = { 5, 5, 7, 8, 8 };

        [MenuItem("Tools/GreenPig/Setup Week2 Core (판정 · 정신력 · 이상현상)")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var sanityConfig = EnsureSanityConfig();
            var stages = EnsureStageConfigs();
            SetupCarPrefab();
            AssetDatabase.SaveAssets();
            SetupCabin(sanityConfig, stages);

            Debug.Log("[GreenPig] 2주차 세팅 완료 — Cabin에서 Play: 0번 칸 → 이상현상 보이면 왼쪽 앞문까지 돌아가기, " +
                      "없으면 계속 오른쪽 → 다 찾고 뒷문. 콘솔에 [Round] / [Sanity] 로그 확인");
        }

        // ───────── Config 에셋 ─────────

        static SanityConfig EnsureSanityConfig()
        {
            Directory.CreateDirectory(ConfigDir);
            string path = $"{ConfigDir}/SanityConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<SanityConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<SanityConfig>();
                AssetDatabase.CreateAsset(cfg, path);
            }
            return cfg;
        }

        static StageConfig[] EnsureStageConfigs()
        {
            var result = new StageConfig[AnomalyCounts.Length];
            for (int i = 0; i < AnomalyCounts.Length; i++)
            {
                int n = i + 1;
                string path = $"{ConfigDir}/Stage{n}.asset";
                var cfg = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
                if (cfg == null)
                {
                    cfg = ScriptableObject.CreateInstance<StageConfig>();
                    cfg.stageNumber = n;
                    cfg.anomalyCount = AnomalyCounts[i];
                    cfg.flashlightEnabled = n >= 3;
                    cfg.ticketCount = 3;
                    AssetDatabase.CreateAsset(cfg, path);
                }
                result[i] = cfg;
            }
            return result;
        }

        // ───────── TrainCar 프리팹 ─────────

        static void SetupCarPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CarPrefabPath);
            try
            {
                var white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);

                // 칸 번호 표지판 (8번 출구처럼 배경 속에 표시 — 11/24 회의, 기획 확정 전 placeholder)
                Remove(root.transform, "CarNumber (placeholder)");
                var numGo = new GameObject("CarNumber (placeholder)");
                numGo.transform.SetParent(root.transform, false);
                numGo.transform.localPosition = new Vector3(0f, 5.2f, 0f);
                var tmp = numGo.AddComponent<TextMeshPro>();
                tmp.text = "0";
                tmp.fontSize = 6f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 1f, 1f, 0.85f);
                tmp.rectTransform.sizeDelta = new Vector2(3f, 1.5f);
                numGo.GetComponent<MeshRenderer>().sortingOrder = -18;

                var car = root.GetComponent<TrainCar>();
                var so = new SerializedObject(car);
                so.FindProperty("numberLabel").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 그림자 있는 임시 승객 (아진 파트 승객 프리팹이 나오면 지우고, 승객 그림자에 CarShadow만 붙이면 됨)
                Remove(root.transform, "PH_Passenger_A");
                Remove(root.transform, "PH_Passenger_B");
                if (white != null)
                {
                    Figure(root.transform, "PH_Passenger_A", white, -4.2f);
                    Figure(root.transform, "PH_Passenger_B", white, 3.6f);
                }
                else Debug.LogWarning($"[GreenPig] {WhitePath} 없음 → 임시 승객 생략 (Build Platform & Cabin Scenes 먼저 실행)");

                PrefabUtility.SaveAsPrefabAsset(root, CarPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void Figure(Transform car, string name, Sprite white, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(car, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            body.transform.localScale = new Vector3(0.9f, 2.2f, 1f);
            var bsr = body.AddComponent<SpriteRenderer>();
            bsr.sprite = white;
            bsr.color = new Color(0.18f, 0.22f, 0.2f, 1f);
            bsr.sortingOrder = -15;

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0.25f, 0.03f, 0f);
            shadow.transform.localScale = new Vector3(1.6f, 0.16f, 1f);
            var ssr = shadow.AddComponent<SpriteRenderer>();
            ssr.sprite = white;
            ssr.color = new Color(0f, 0f, 0f, 0.45f);
            ssr.sortingOrder = -16;
            shadow.AddComponent<CarShadow>();
        }

        // ───────── Cabin 씬 ─────────

        static void SetupCabin(SanityConfig sanityConfig, StageConfig[] stages)
        {
            var scene = EditorSceneManager.OpenScene(CabinScene);

            // 씬을 열면 그 전에 불러온 에셋 참조가 끊길 수 있어서 경로로 다시 불러옴 (TrainBuilderTool과 같은 이유)
            sanityConfig = AssetDatabase.LoadAssetAtPath<SanityConfig>($"{ConfigDir}/SanityConfig.asset");
            for (int i = 0; i < stages.Length; i++)
                stages[i] = AssetDatabase.LoadAssetAtPath<StageConfig>($"{ConfigDir}/Stage{i + 1}.asset");

            foreach (var old in new[] { "[Game]", "Anomalies" })
            {
                var go = GameObject.Find(old);
                if (go != null) Object.DestroyImmediate(go);
            }

            var train = Object.FindAnyObjectByType<TrainBuilder>();
            if (train == null)
            {
                Debug.LogError("[GreenPig] Cabin에 TrainBuilder 없음 → Tools > GreenPig > Build Train 먼저 실행");
                return;
            }
            var trainSo = new SerializedObject(train);
            trainSo.FindProperty("buildOnAwake").boolValue = false; // GameManager가 스테이지 칸 수로 생성
            trainSo.ApplyModifiedPropertiesWithoutUndo();

            var anomaliesGo = new GameObject("Anomalies");
            var adSwap = anomaliesGo.AddComponent<AdSwapAnomaly>();
            var noShadow = anomaliesGo.AddComponent<ShadowlessAnomaly>();

            var gameGo = new GameObject("[Game]");
            var sanity = gameGo.AddComponent<SanitySystem>();
            var round = gameGo.AddComponent<RoundManager>();
            var gm = gameGo.AddComponent<GameManager>();

            var player = Object.FindAnyObjectByType<PlayerController>();
            var flashlight = Object.FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);

            var sSo = new SerializedObject(sanity);
            sSo.FindProperty("config").objectReferenceValue = sanityConfig;
            sSo.ApplyModifiedPropertiesWithoutUndo();

            var rSo = new SerializedObject(round);
            rSo.FindProperty("train").objectReferenceValue = train;
            rSo.FindProperty("sanity").objectReferenceValue = sanity;
            rSo.FindProperty("player").objectReferenceValue = player;
            var list = rSo.FindProperty("anomalies");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = adSwap;
            list.GetArrayElementAtIndex(1).objectReferenceValue = noShadow;
            rSo.ApplyModifiedPropertiesWithoutUndo();

            var gSo = new SerializedObject(gm);
            var arr = gSo.FindProperty("stages");
            arr.arraySize = stages.Length;
            for (int i = 0; i < stages.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            gSo.FindProperty("train").objectReferenceValue = train;
            gSo.FindProperty("sanity").objectReferenceValue = sanity;
            gSo.FindProperty("round").objectReferenceValue = round;
            gSo.FindProperty("player").objectReferenceValue = player;
            gSo.FindProperty("flashlight").objectReferenceValue = flashlight;
            gSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void Remove(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
    }
}
#endif
