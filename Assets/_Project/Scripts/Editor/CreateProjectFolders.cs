#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace GreenPig.EditorTools
{
    /// <summary>
    /// 메뉴 Tools > GreenPig > Create Folder Structure 로 합의된 폴더 구조를 한 번에 생성.
    /// 빈 폴더는 Git에 안 올라가므로 각 폴더에 .gitkeep 파일을 넣는다.
    /// </summary>
    public static class CreateProjectFolders
    {
        static readonly string[] Folders =
        {
            "_Project/Scenes",
            "_Project/Scripts/Shared",
            "_Project/Scripts/Core",
            "_Project/Scripts/World/Player",
            "_Project/Scripts/World/Train",
            "_Project/Scripts/World/Anomalies",
            "_Project/Scripts/UI/Dialogue",
            "_Project/Scripts/UI/HUD",
            "_Project/Scripts/UI/Menus",
            "_Project/Scripts/Minigames/Ticketing",
            "_Project/Scripts/Minigames/Tasks",
            "_Project/Scripts/Passengers",
            "_Project/Scripts/Editor",
            "_Project/Prefabs/World",
            "_Project/Prefabs/UI",
            "_Project/Art/Sprites",
            "_Project/Art/Backgrounds",
            "_Project/Art/UI",
            "_Project/Audio/BGM",
            "_Project/Audio/SFX",
            "_Project/Data/Dialogue",
            "_Project/Data/Passengers",
            "_Project/Data/Config",
        };

        [MenuItem("Tools/GreenPig/Create Folder Structure")]
        public static void Create()
        {
            foreach (var folder in Folders)
            {
                var path = Path.Combine("Assets", folder);
                Directory.CreateDirectory(path);
                var keep = Path.Combine(path, ".gitkeep");
                if (!File.Exists(keep)) File.WriteAllText(keep, "");
            }
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[GreenPig] 폴더 구조 생성 완료");
        }
    }
}
#endif
