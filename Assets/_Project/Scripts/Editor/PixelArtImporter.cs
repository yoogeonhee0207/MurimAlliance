using UnityEditor;
using UnityEngine;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// Assets/_Project/Art 아래 이미지에 도트용 가져오기 설정을 자동 적용한다.
    /// ThirdParty 등 다른 폴더의 이미지는 건드리지 않는다.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        const string ArtRoot = "Assets/_Project/Art/";
        const int PixelsPerUnit = 32;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;

            // 처음 들어온 이미지만 스프라이트(단일)로 지정. 이후 시트로 바꾼 설정은 유지.
            if (importer.importSettingsMissing)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
            }

            // 도트가 흐려지거나 번지지 않게 항상 적용
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            // 배경 타일은 반복해서 깔 수 있게 Full Rect + Repeat
            if (assetPath.Contains("/Backgrounds/"))
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.wrapMode = TextureWrapMode.Repeat;
            }
        }
    }
}
