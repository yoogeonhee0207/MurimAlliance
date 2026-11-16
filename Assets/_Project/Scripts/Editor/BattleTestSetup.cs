using MurimAlliance.Battle;
using MurimAlliance.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// 1대1 전투 테스트 씬을 만든다. 메뉴: Tools > 천하무림맹 > 전투 테스트 씬 (1대1)
    /// </summary>
    public static class BattleTestSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/BattleTest.unity";

        [MenuItem("Tools/천하무림맹/전투 테스트 씬 (1대1)")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / 2f / 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(40, 48, 72, 255);
            camGo.transform.position = new Vector3(0, 0, -10);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 32; ppc.refResolutionX = 480; ppc.refResolutionY = 270;

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;

            var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Backgrounds/ph_floor_tile.png");
            var floor = new GameObject("Floor").AddComponent<SpriteRenderer>();
            floor.sprite = floorSprite; floor.drawMode = SpriteDrawMode.Tiled;
            floor.size = new Vector2(15f, 8.4375f); floor.sortingOrder = -10000;

            var spawner = new GameObject("BattleTestSpawner").AddComponent<BattleTestSpawner>();
            spawner.allies = new[] { Load("ally_shaolin_brawler") };
            spawner.enemies = new[] { Load("enemy_bandit") };
            spawner.shadowNormal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Effects/ph_shadow.png");
            spawner.shadowLarge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Effects/ph_shadow_large.png");

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BattleTest] 테스트 씬 생성: {ScenePath} (소림 권사 vs 산적)");
        }

        static UnitData Load(string name) =>
            AssetDatabase.LoadAssetAtPath<UnitData>($"Assets/_Project/Data/Units/{name}.asset");
    }
}
