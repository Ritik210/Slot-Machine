using UnityEditor;
using UnityEngine;
using System.IO;

public class SpriteExtractor
{
    [MenuItem("Tools/Extract Sprites From Atlas")]
    static void Extract()
    {
        Texture2D texture = Selection.activeObject as Texture2D;
        if (texture == null)
        {
            Debug.LogError("Select the atlas PNG first.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);

        string outputDir = "Assets/ExtractedSprites";
        Directory.CreateDirectory(outputDir);

        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                Texture2D tex = new Texture2D(
                    (int)sprite.rect.width,
                    (int)sprite.rect.height,
                    TextureFormat.RGBA32,
                    false
                );

                var pixels = sprite.texture.GetPixels(
                    (int)sprite.rect.x,
                    (int)sprite.rect.y,
                    (int)sprite.rect.width,
                    (int)sprite.rect.height
                );

                tex.SetPixels(pixels);
                tex.Apply();

                File.WriteAllBytes(
                    $"{outputDir}/{sprite.name}.png",
                    tex.EncodeToPNG()
                );
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Sprites extracted to Assets/ExtractedSprites");
    }
}
