using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// 현재 열린 씬의 메인 카메라에 도트용 Pixel Perfect Camera를 설정한다.
    /// 메뉴: Tools > 천하무림맹 > 픽셀 카메라 설정
    /// </summary>
    public static class PixelCameraSetup
    {
        const int PixelsPerUnit = 32;
        const int RefWidth = 640;
        const int RefHeight = 360;
        const string TestSpritePath = "Assets/_Project/Art/Characters/_test_pixel.png";

        [MenuItem("Tools/천하무림맹/픽셀 카메라 설정")]
        public static void SetupPixelCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[PixelCameraSetup] Main Camera 태그가 붙은 카메라가 없습니다.");
                return;
            }

            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = Undo.AddComponent<PixelPerfectCamera>(cam.gameObject);

            Undo.RecordObject(ppc, "Pixel Perfect Camera 설정");
            ppc.assetsPPU = PixelsPerUnit;
            ppc.refResolutionX = RefWidth;
            ppc.refResolutionY = RefHeight;

            EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
            EditorSceneManager.SaveScene(cam.gameObject.scene);
            Debug.Log($"[PixelCameraSetup] {cam.name}: {RefWidth}x{RefHeight}, {PixelsPerUnit} PPU 적용 완료");
        }

        [MenuItem("Tools/천하무림맹/테스트 스프라이트 배치")]
        public static void PlaceTestSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TestSpritePath);
            if (sprite == null)
            {
                Debug.LogError($"[PixelCameraSetup] 테스트 스프라이트를 찾을 수 없습니다: {TestSpritePath}");
                return;
            }

            var go = GameObject.Find("_TestPixelSprite");
            if (go == null) go = new GameObject("_TestPixelSprite");
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            go.transform.position = Vector3.zero;

            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveScene(go.scene);
            Debug.Log("[PixelCameraSetup] 테스트 스프라이트 배치 완료");
        }

        [MenuItem("Tools/천하무림맹/테스트 스프라이트 제거")]
        public static void RemoveTestSprite()
        {
            var go = GameObject.Find("_TestPixelSprite");
            if (go == null)
            {
                Debug.Log("[PixelCameraSetup] 씬에 테스트 스프라이트가 없습니다.");
                return;
            }

            var scene = go.scene;
            Undo.DestroyObjectImmediate(go);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PixelCameraSetup] 테스트 스프라이트 제거 완료");
        }
    }
}
