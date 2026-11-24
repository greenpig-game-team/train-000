#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GreenPig.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace GreenPig.EditorTools
{
    /// <summary>
    /// Tools > GreenPig > Setup Player (Placeholder)
    /// 1) 걷기 스프라이트 시트를 12프레임으로 자동 슬라이스
    /// 2) 현재 씬에 Player 오브젝트 생성 + 카메라 follow 연결
    /// </summary>
    public static class PlayerSetupTool
    {
        const string SheetPath = "Assets/_Project/Art/Sprites/Player/Player_Walk_Sheet.png";
        const int FrameCount = 12;

        [MenuItem("Tools/GreenPig/Setup Player (Placeholder)")]
        public static void Setup()
        {
            var sprites = SliceSheet();
            if (sprites == null) return;
            CreatePlayer(sprites);
        }

        static Sprite[] SliceSheet()
        {
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[GreenPig] 스프라이트 시트를 찾을 수 없음: {SheetPath}");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            int fw = tex.width / FrameCount, fh = tex.height;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
            dp.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < FrameCount; i++)
            {
                var r = new SpriteRect
                {
                    name = $"Player_Walk_{i:00}",
                    rect = new Rect(i * fw, 0, fw, fh),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate(),
                };
                rects.Add(r);
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            }
            dp.SetSpriteRects(rects.ToArray());
            dp.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(pairs);
            dp.Apply();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(SheetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
        }

        static void CreatePlayer(Sprite[] frames)
        {
            var existing = GameObject.Find("Player");
            if (existing != null) Object.DestroyImmediate(existing);

            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = frames[2];
            sr.sortingOrder = 10;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 1.9f);
            col.offset = new Vector2(0f, 0.95f);

            var anim = player.AddComponent<SpriteFrameAnimator>();
            anim.SetFrames(frames, 2);

            player.AddComponent<PlayerController>();

            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<CameraFollow2D>();
                if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();
                follow.SetTarget(player.transform);
                var p = cam.transform.position;
                cam.transform.position = new Vector3(0f, 1f, p.z);
            }

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log("[GreenPig] Player 생성 완료 — Play 후 A/D로 이동");
        }
    }
}
#endif
