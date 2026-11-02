using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IdleBlade.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("IdleBlade/Create Boot Scene")]
        public static void Create()
        {
            const string path = "Assets/Scenes/Boot.unity";
            if (File.Exists(path)) { EditorSceneManager.OpenScene(path); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("IdleBlade").AddComponent<IdleGame>();
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            PlayerSettings.companyName = "IndependentStudio";
            PlayerSettings.productName = "IdleBlade Prototype";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            AssetDatabase.SaveAssets();
        }
    }
}
