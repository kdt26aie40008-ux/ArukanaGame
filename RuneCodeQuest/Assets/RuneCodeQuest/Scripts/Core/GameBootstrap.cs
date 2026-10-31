using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// ゲームの起動係。
    /// どのシーンで Play を押しても、GameManager が無ければ自動で作ります。
    /// （素材やプレハブが無くても動作確認できるようにするための仕組み）
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            new GameObject("GameManager").AddComponent<GameManager>();
        }
    }
}
