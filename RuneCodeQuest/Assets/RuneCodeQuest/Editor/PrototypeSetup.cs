using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RuneCodeQuest.EditorTools
{
    /// <summary>
    /// エディタ用の準備スクリプト。
    /// プロジェクトを初めて開いたときに Main シーンを作り、ビルド設定に登録して開きます。
    /// メニュー「RuneCodeQuest > メインシーンを作り直す」からも実行できます。
    ///
    /// シーンにはカメラだけを置きます。ゲームの中身は Play したときに
    /// GameBootstrap が自動で作るので、シーンを編集しなくても遊べます。
    /// </summary>
    [InitializeOnLoad]
    public static class PrototypeSetup
    {
        private const string SceneDir = "Assets/RuneCodeQuest/Scenes";
        private const string ScenePath = SceneDir + "/Main.unity";

        static PrototypeSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (File.Exists(ScenePath)) return;
                CreateMainScene();
            };
        }

        [MenuItem("RuneCodeQuest/メインシーンを作り直す")]
        public static void CreateMainScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.09f);
            camGo.transform.position = new Vector3(9.5f, 5.8f, -10f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.Refresh();
            Debug.Log("[RuneCodeQuest] Main シーンを作成しました。Play ボタンで遊べます。");
        }
    }
}
