#if UNITY_EDITOR
using GreenPig.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GreenPig.EditorTools
{
    /// <summary>
    /// Tools > GreenPig > Build Train (0번 칸 + n칸)
    /// 1) 칸 1개짜리 TrainCar 프리팹(배경 + 광고판 분리) 생성
    /// 2) Cabin 씬의 예전 열차(Train_3Cars / Environment)를 지우고 TrainBuilder(임시 n = 2 → 세 칸)를 배치
    ///    → Play 시 0번 칸(기준) + n칸이 생성되고 카메라 경계가 열차 전체 길이로 설정됨
    /// </summary>
    public static class TrainBuilderTool
    {
        const string BgPath = "Assets/_Project/Art/Backgrounds/BG_Cabin_Stage1.jpg";
        const string AdLeftPath = "Assets/_Project/Art/Backgrounds/Ad_Left.png";
        const string AdRightPath = "Assets/_Project/Art/Backgrounds/Ad_Right.png";
        const string CarPrefabPath = "Assets/_Project/Prefabs/World/TrainCar.prefab";
        const string CabinScene = "Assets/_Project/Scenes/Cabin.unity";

        // SceneSetupTool과 같은 기준 (러프 1000x400px, 카메라 세로 7 units)
        const float CamSize = 3.5f;
        const float BgPpu = 400f / (CamSize * 2f);
        const float CamY = CamSize - 55f / BgPpu;
        /// <summary>칸 1개 폭 (러프 1000px 기준, units)</summary>
        public const float CarWidth = 1000f / BgPpu;

        // 임시: 지금은 세 칸만 (0번 기준 칸 + 2칸). 스테이지 1 확정값은 5
        const int Stage1AnomalyCount = 2;

        // 러프 이미지 안에서 광고판 중심 (px, 왼쪽 위 기준)
        static readonly Vector2 AdLeftPx = new(152f, 115f);
        static readonly Vector2 AdRightPx = new(808.5f, 117f);

        [MenuItem("Tools/GreenPig/Build Train (0번 칸 + n칸)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var bg = ImportSprite(BgPath);
            var adL = ImportSprite(AdLeftPath);
            var adR = ImportSprite(AdRightPath);
            if (bg == null || adL == null || adR == null) return;

            BuildCarPrefab(bg, adL, adR);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateCabinScene(bg);
            Debug.Log($"[GreenPig] 열차 세팅 완료 — Cabin에서 Play 시 0번 칸 + {Stage1AnomalyCount}칸 생성. " +
                      "예전 Train_3Cars.prefab은 더 이상 안 쓰니 지워도 됨");
        }

        static void BuildCarPrefab(Sprite bg, Sprite adL, Sprite adR)
        {
            var root = new GameObject("TrainCar");

            var bgGo = new GameObject("Background (러프)");
            bgGo.transform.SetParent(root.transform);
            bgGo.transform.localPosition = new Vector3(0f, CamY, 0f);
            var sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = bg;
            sr.sortingOrder = -20;

            Ad(root.transform, "AdBoard_Left", adL, AdLeftPx);
            Ad(root.transform, "AdBoard_Right", adR, AdRightPx);

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 3f);
            col.size = new Vector2(CarWidth, 10f);

            var car = root.AddComponent<TrainCar>();
            var so = new SerializedObject(car);
            so.FindProperty("background").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CarPrefabPath);
            Object.DestroyImmediate(root);
        }

        static void UpdateCabinScene(Sprite bg)
        {
            var scene = EditorSceneManager.OpenScene(CabinScene);

            // 씬을 연 뒤에 프리팹을 다시 불러와야 참조가 끊기지 않음
            var carPrefab = AssetDatabase.LoadAssetAtPath<TrainCar>(CarPrefabPath);
            if (carPrefab == null)
            {
                Debug.LogError($"[GreenPig] 칸 프리팹을 못 찾음: {CarPrefabPath}");
                return;
            }

            foreach (var oldName in new[] { "Environment", "Train_3Cars", "Train" })
            {
                var old = GameObject.Find(oldName);
                if (old != null) Object.DestroyImmediate(old);
            }

            var trainGo = new GameObject("Train");
            var builder = trainGo.AddComponent<TrainBuilder>();
            var so = new SerializedObject(builder);
            so.FindProperty("carPrefab").objectReferenceValue = carPrefab;
            so.FindProperty("anomalyCarCount").intValue = Stage1AnomalyCount;
            var bgs = so.FindProperty("backgrounds");
            bgs.arraySize = 1; // 배경 placeholder가 더 생기면 인스펙터에 추가 → 칸마다 돌려가며 사용
            bgs.GetArrayElementAtIndex(0).objectReferenceValue = bg;
            so.ApplyModifiedPropertiesWithoutUndo();

            float minX = -CarWidth * 0.5f;
            float maxX = minX + CarWidth * (Stage1AnomalyCount + 1);

            // 플레이어는 0번(기준) 칸 뒤쪽에서 출발
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null) player.transform.position = new Vector3(minX + 3f, 0f, 0f);

            var follow = Object.FindAnyObjectByType<CameraFollow2D>();
            if (follow != null)
            {
                follow.SetBounds(minX, maxX);
                if (player != null)
                {
                    var p = follow.transform.position;
                    follow.transform.position = new Vector3(player.transform.position.x, p.y, p.z);
                }
                EditorUtility.SetDirty(follow);
            }

            // 손전등 테스트 그림자는 0번(기준) 칸이 아니라 1번 칸으로
            var test = GameObject.Find("TEST_FlashlightShadow");
            if (test != null)
            {
                var p = test.transform.position;
                test.transform.position = new Vector3(CarWidth, p.y, p.z);
                EditorUtility.SetDirty(test);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ───────── Helpers ─────────

        static Sprite ImportSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogError($"[GreenPig] 이미지 없음: {path}");
                return null;
            }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = BgPpu;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Ad(Transform car, string name, Sprite sprite, Vector2 px)
        {
            var go = new GameObject(name);
            go.transform.SetParent(car);
            go.transform.localPosition = new Vector3((px.x - 500f) / BgPpu, CamY + (200f - px.y) / BgPpu, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -19;
            go.AddComponent<AdBoard>(); // id는 TrainCar.Setup에서 "Car{번호}_Left/Right"로 지정
        }
    }
}
#endif
