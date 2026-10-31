using UnityEditor;

namespace RuneCodeQuest
{
    /// <summary>
    /// Resources/Art/ に入れたドット絵の読みこみ設定を、自動でそろえる。
    /// 画像生成AIの絵は1体が100〜150ドットと大きいので、縮めて表示してもきれいに見えるように
    /// なめらか（Bilinear）＋ミップマップ・圧縮しない・2のべき乗にしない・透明のふちをきれいに。
    /// </summary>
    public class ArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = UnityEngine.FilterMode.Bilinear;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.mipmapEnabled = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            // パーツきせかえの部品は、ゲームの中で重ねて1枚にするので 読み取りできるようにする（ミップマップはいらない）
            if (assetPath.Replace('\\', '/').Contains("/Resources/Art/CharacterParts/"))
            {
                ti.isReadable = true;
                ti.mipmapEnabled = false;
            }
        }
    }
}
