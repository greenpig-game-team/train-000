#if UNITY_EDITOR
using GreenPig.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GreenPig.EditorTools
{
    /// <summary>
    /// Tools > GreenPig > Build 3-Car Train
    /// 객실 러프 3장을 이어 붙인 3칸 열차 프리팹(광고판 분리, 앞/뒷문 트리거, 양끝 벽)을 만들고
    /// Cabin 씬의 임시 배경을 이 프리팹으로 교체 + 카메라 경계를 열차 전체 길이로 설정.
    /// </summary>
    public static class TrainBuilderTool
    {
        const string BgPath = "Assets/_Project/Art/Backgrounds/BG_Cabin_Stage1.jpg";
        const string AdLeftPath = "Assets/_Project/Art/Backgrounds/Ad_Left.png";
        const string AdRightPath = "Assets/_Project/Art/Backgrounds/Ad_Right.png";
        const string PrefabPath = "Assets/_Project/Prefabs/World/Train_3Cars.prefab";
        const string CabinScene = "Assets/_Project/Scenes/Cabin.unity";

        // SceneSetupTool과 같은 기준 (러프 1000x400px, 카메라 세로 7 units)
        const float CamSize = 3.5f;
        const float BgPpu = 400f / (CamSize * 2f);
        const float CamY = CamSize - 55f / BgPpu;
        const int CarCount = 3;

        // 러프 이미지 안에서 광고판 중심 (px, 왼쪽 위 기준)
        static readonly Vector2 AdLeftPx = new(152f, 115f);
        static readonly Vector2 AdRightPx = new(808.5f, 117f);

        [MenuItem("Tools/GreenPig/Build 3-Car Train")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var bg = ImportSprite(BgPath);
            var adL = ImportSprite(AdLeftPath);
            var adR = ImportSprite(AdRightPath);
            if (bg == null || adL == null || adR == null) return;

            float carW = bg.bounds.size.x;
            float half = carW * CarCount * 0.5f;

            var prefab = BuildPrefab(bg, adL, adR, carW, half);
            UpdateCabinScene(prefab, half);
            Debug.Log($"[GreenPig] 3칸 열차 생성 완료 (길이 {half * 2f:0.0} units) — Cabin 씬 업데이트됨");
        }

        static GameObject BuildPrefab(Sprite bg, Sprite adL, Sprite adR, float carW, float half)
        {
            var root = new GameObject("Train_3Cars");

            for (int i = 0; i < CarCount; i++)
            {
                float cx = -half + carW * (i + 0.5f);
                var car = new GameObject($"Car_{i + 1}");
                car.transform.SetParent(root.transform);
                car.transform.localPosition = new Vector3(cx, 0f, 0f);

                var bgGo = new GameObject("Background (러프)");
                bgGo.transform.SetParent(car.transform);
                bgGo.transform.localPosition = new Vector3(0f, CamY, 0f);
                var sr = bgGo.AddComponent<SpriteRenderer>();
                sr.sprite = bg;
                sr.sortingOrder = -20;

                Ad(car.transform, "AdBoard_Left", adL, AdLeftPx, $"Car{i + 1}_Left");
                Ad(car.transform, "AdBoard_Right", adR, AdRightPx, $"Car{i + 1}_Right");
            }

            EndTrigger(root.transform, "Door_Back (뒷문)", TrainEndTrigger.Side.Back, -half + 1f);
            EndTrigger(root.transform, "Door_Front (앞문)", TrainEndTrigger.Side.Front, half - 1f);
            Wall(root.transform, "Wall_Back", -half - 0.5f);
            Wall(root.transform, "Wall_Front", half + 0.5f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void UpdateCabinScene(GameObject trainPrefab, float half)
        {
            var scene = EditorSceneManager.OpenScene(CabinScene);

            var oldEnv = GameObject.Find("Environment");
            if (oldEnv != null) Object.DestroyImmediate(oldEnv);
            var oldTrain = GameObject.Find("Train_3Cars");
            if (oldTrain != null) Object.DestroyImmediate(oldTrain);

            PrefabUtility.InstantiatePrefab(trainPrefab);

            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null) player.transform.position = new Vector3(-half + 3f, 0f, 0f);

            var follow = Object.FindAnyObjectByType<CameraFollow2D>();
            if (follow != null)
            {
                follow.SetBounds(-half, half);
                if (player != null)
                {
                    var p = follow.transform.position;
                    follow.transform.position = new Vector3(player.transform.position.x, p.y, p.z);
                }
                EditorUtility.SetDirty(follow);
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

        static void Ad(Transform car, string name, Sprite sprite, Vector2 px, string id)
        {
            var go = new GameObject(name);
            go.transform.SetParent(car);
            go.transform.localPosition = new Vector3((px.x - 500f) / BgPpu, CamY + (200f - px.y) / BgPpu, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -19;
            go.AddComponent<AdBoard>().SetId(id);
        }

        static void EndTrigger(Transform parent, string name, TrainEndTrigger.Side side, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(x, 3f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 10f);
            go.AddComponent<TrainEndTrigger>().SetSide(side);
        }

        static void Wall(Transform parent, string name, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(x, 3f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 10f);
        }
    }
}
#endif
