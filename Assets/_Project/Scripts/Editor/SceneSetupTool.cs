#if UNITY_EDITOR
using System.IO;
using System.Linq;
using GreenPig.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GreenPig.EditorTools
{
    /// <summary>
    /// Tools > GreenPig > Build Platform & Cabin Scenes
    /// Player 프리팹 + Platform(오프닝 플랫폼) / Cabin(객실) placeholder 씬을 생성하고 Build Settings에 등록.
    /// 아트가 나오면 배경 오브젝트(Background_*)의 스프라이트만 교체하면 됨.
    /// </summary>
    public static class SceneSetupTool
    {
        const string SheetPath = "Assets/_Project/Art/Sprites/Player/Player_Walk_Sheet.png";
        const string WhitePath = "Assets/_Project/Art/Sprites/Placeholder/White.png";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/World/Player.prefab";
        const string PlatformScene = "Assets/_Project/Scenes/Platform.unity";
        const string CabinScene = "Assets/_Project/Scenes/Cabin.unity";
        const string CabinBgPath = "Assets/_Project/Art/Backgrounds/BG_Cabin_Stage1.jpg";

        // 배경 러프(1000x400px)가 카메라 세로(7 units)를 꽉 채우도록 맞춘 값
        const float CamSize = 3.5f;                 // orthographic size → 화면 세로 7 units
        const float BgPpu = 400f / (CamSize * 2f);   // 57.14 px/unit
        const float BgFloorPx = 55f;                 // 러프 이미지 아래에서 바닥선까지 px
        const float FloorOffset = BgFloorPx / BgPpu; // 0.96 units
        const float CamY = CamSize - FloorOffset;    // 바닥(y=0)이 러프 바닥선에 오도록
        const float PlayerScale = 2.4f;              // 러프 속 좌석/사람 비율에 맞춘 주인공 크기
        const float PlayerSpeed = 4f;

        [MenuItem("Tools/GreenPig/Build Platform & Cabin Scenes")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var white = EnsureWhiteSprite();
            var playerPrefab = BuildPlayerPrefab();
            if (playerPrefab == null) return;

            BuildPlatform(white, playerPrefab);
            BuildCabin(white, playerPrefab);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(PlatformScene, true),
                new EditorBuildSettingsScene(CabinScene, true),
            };

            EditorSceneManager.OpenScene(PlatformScene);
            Debug.Log("[GreenPig] Platform / Cabin 씬 생성 완료 — Platform에서 Play, 문 앞에서 E");
        }

        // ───────── Assets ─────────

        static Sprite EnsureWhiteSprite()
        {
            if (!File.Exists(WhitePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(WhitePath));
                var tex = new Texture2D(4, 4);
                tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
                File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(WhitePath);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(WhitePath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 4; // 1x1 unit
            imp.filterMode = FilterMode.Point;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
        }

        static Sprite EnsureBackground(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning($"[GreenPig] 배경 러프 없음: {path}");
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

        static GameObject BuildPlayerPrefab()
        {
            var frames = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length == 0)
            {
                Debug.LogError("[GreenPig] 걷기 스프라이트가 없음. 먼저 Tools > GreenPig > Setup Player 실행");
                return null;
            }

            var go = new GameObject("Player");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[Mathf.Min(2, frames.Length - 1)];
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 1.9f);
            col.offset = new Vector2(0f, 0.95f);

            go.AddComponent<SpriteFrameAnimator>().SetFrames(frames, 2);
            var pc = go.AddComponent<PlayerController>();
            go.AddComponent<PlayerInteractor>();
            go.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);
            var so = new SerializedObject(pc);
            so.FindProperty("moveSpeed").floatValue = PlayerSpeed;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PlayerPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ───────── Scenes ─────────

        static void BuildPlatform(Sprite white, GameObject playerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCameraAndLight(new Color(0.05f, 0.06f, 0.1f));

            var env = new GameObject("Environment").transform;
            Rect(white, "Background_PlatformWall", env, new Vector2(10f, CamY), new Vector2(60f, 7f), new Color(0.12f, 0.13f, 0.17f), -20);
            Rect(white, "Floor_Platform", env, new Vector2(10f, -0.5f), new Vector2(60f, 1f), new Color(0.35f, 0.35f, 0.38f), -5);
            Rect(white, "Platform_YellowLine", env, new Vector2(10f, -0.05f), new Vector2(60f, 0.1f), new Color(0.85f, 0.75f, 0.2f), -4);
            Rect(white, "Background_TrainBody", env, new Vector2(21.5f, 3.1f), new Vector2(37f, 5.8f), new Color(0.16f, 0.32f, 0.22f), -10);

            Wall("Wall_Left", env, -11f);
            Wall("Wall_Right", env, 12f);

            var door = Rect(white, "TrainDoor", null, new Vector2(6f, 2.7f), new Vector2(2.8f, 5.4f), new Color(0.55f, 0.65f, 0.6f), -8);
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.4f, 1f); // 문 폭보다 조금 넓게
            door.AddComponent<TrainDoor>();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(-5f, 0f, 0f);
            Follow(cam, player.transform);

            EditorSceneManager.SaveScene(scene, PlatformScene);
        }

        static void BuildCabin(Sprite white, GameObject playerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCameraAndLight(new Color(0.03f, 0.05f, 0.04f));

            var bg = EnsureBackground(CabinBgPath);
            float halfW = bg != null ? bg.bounds.size.x * 0.5f : 8.75f;

            var env = new GameObject("Environment").transform;
            if (bg != null)
            {
                var bgGo = new GameObject("Background_Cabin_Stage1 (러프)");
                bgGo.transform.SetParent(env);
                bgGo.transform.position = new Vector3(0f, CamY, 0f);
                var bgSr = bgGo.AddComponent<SpriteRenderer>();
                bgSr.sprite = bg;
                bgSr.sortingOrder = -20;
            }

            Wall("Wall_Left", env, -halfW - 0.5f);
            Wall("Wall_Right", env, halfW + 0.5f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(-halfW + 2.5f, 0f, 0f); // 객실 입구 쪽
            var follow = Follow(cam, player.transform);
            follow.SetBounds(-halfW, halfW);

            EditorSceneManager.SaveScene(scene, CabinScene);
        }

        // ───────── Helpers ─────────

        static Camera CreateCameraAndLight(Color bg)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            camGo.transform.position = new Vector3(0f, CamY, -10f);
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            return cam;
        }

        static CameraFollow2D Follow(Camera cam, Transform target)
        {
            var f = cam.gameObject.AddComponent<CameraFollow2D>();
            f.SetTarget(target);
            var p = cam.transform.position;
            cam.transform.position = new Vector3(target.position.x, p.y, p.z);
            return f;
        }

        /// <summary>플레이어가 화면 밖으로 못 나가게 막는 투명 벽</summary>
        static void Wall(string name, Transform parent, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, 3f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 10f);
        }

        static GameObject Rect(Sprite white, string name, Transform parent, Vector2 center, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = white;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }
    }
}
#endif
