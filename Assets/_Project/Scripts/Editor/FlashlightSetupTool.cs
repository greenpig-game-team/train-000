#if UNITY_EDITOR
using System.Linq;
using GreenPig.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GreenPig.EditorTools
{
    /// <summary>
    /// Tools > GreenPig > Setup Flashlight
    /// Player 프리팹에 손전등(Light2D + FlashlightController) 추가,
    /// 씬 조명은 기본 밝기 유지, Cabin 2번 칸에 손전등 테스트용 "숨은 그림자" 배치.
    /// </summary>
    public static class FlashlightSetupTool
    {
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/World/Player.prefab";
        const string WhitePath = "Assets/_Project/Art/Sprites/Placeholder/White.png";
        const string PlatformScene = "Assets/_Project/Scenes/Platform.unity";
        const string CabinScene = "Assets/_Project/Scenes/Cabin.unity";

        // 배경 밝기는 그대로 유지 — 손전등은 화면을 밝히는 용도가 아니라 특수 이상현상(FlashlightTarget)을 찾는 용도
        const float CabinAmbient = 1f;
        const float PlatformAmbient = 1f;

        [MenuItem("Tools/GreenPig/Setup Flashlight")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AddFlashlightToPlayer();
            SetAmbient(PlatformScene, PlatformAmbient, addTest: false);
            SetAmbient(CabinScene, CabinAmbient, addTest: true);

            Debug.Log("[GreenPig] 손전등 세팅 완료 — Cabin에서 C키, 2번 칸 가운데에 숨은 그림자 테스트");
        }

        static void AddFlashlightToPlayer()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            var old = root.transform.Find("Flashlight");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var oldCtrl = root.GetComponent<FlashlightController>();
            if (oldCtrl != null) Object.DestroyImmediate(oldCtrl);

            var lightGo = new GameObject("Flashlight");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            // AddComponent로 만든 Light2D는 비출 Sorting Layer가 비어 있어서 아무것도 안 밝힘 → 전체 레이어 지정
            var lso = new SerializedObject(light);
            var layers = lso.FindProperty("m_ApplyToSortingLayers");
            var ids = SortingLayer.layers.Select(l => l.id).ToArray();
            layers.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++) layers.GetArrayElementAtIndex(i).intValue = ids[i];
            lso.ApplyModifiedPropertiesWithoutUndo();

            var ctrl = root.AddComponent<FlashlightController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("player").objectReferenceValue = root.GetComponent<PlayerController>();
            so.FindProperty("beam").objectReferenceValue = light;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void SetAmbient(string scenePath, float intensity, bool addTest)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);

            var global = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
            if (global != null)
            {
                global.intensity = intensity;
                EditorUtility.SetDirty(global);
            }

            if (addTest)
            {
                var old = GameObject.Find("TEST_FlashlightShadow");
                if (old != null) Object.DestroyImmediate(old);

                var go = new GameObject("TEST_FlashlightShadow");
                go.transform.position = new Vector3(0f, 2.2f, 0f); // 2번 칸 가운데, 사람 키 정도
                go.transform.localScale = new Vector3(1.2f, 4.4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
                sr.color = new Color(0.35f, 0.05f, 0.05f, 1f);
                sr.sortingOrder = -15;
                go.AddComponent<FlashlightTarget>().SetRevealOnly(true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
