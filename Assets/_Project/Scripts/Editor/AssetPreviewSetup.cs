using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// 임시(ph_) 에셋을 한 화면에 늘어놓은 확인용 씬을 만든다.
    /// 메뉴: Tools > 천하무림맹 > 임시 에셋 미리보기 씬
    /// </summary>
    public static class AssetPreviewSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/AssetPreview.unity";
        const string Art = "Assets/_Project/Art/";

        [MenuItem("Tools/천하무림맹/임시 에셋 미리보기 씬")]
        public static void CreatePreviewScene()
        {
            // 배경 타일이 최신 가져오기 설정(Full Rect)을 쓰도록 다시 가져오기
            AssetDatabase.ImportAsset(Art + "Backgrounds/ph_floor_tile.png", ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라 (480x270, 32 PPU)
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / 2f / 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(40, 48, 72, 255);
            camGo.transform.position = new Vector3(0, 0, -10);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 32; ppc.refResolutionX = 480; ppc.refResolutionY = 270;

            // 2D 전역 조명 (없으면 스프라이트가 어둡게 보임)
            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;

            // 바닥
            var floor = Place("Backgrounds/ph_floor_tile", new Vector2(0, 0), -10);
            if (floor != null)
            {
                floor.drawMode = SpriteDrawMode.Tiled;
                floor.size = new Vector2(15f, 8.4375f);
            }

            // 1줄: 아군
            var allies = new[] { "ph_shaolin_brawler", "ph_huashan_swordsman", "ph_wudang_swordsman", "ph_wudang_taoist", "ph_tang_assassin", "ph_tang_healer" };
            for (int i = 0; i < allies.Length; i++) Character("Characters/" + allies[i], new Vector2(-6.25f + i * 2.5f, 3f));

            // 2줄: 적 + 보스
            Character("Enemies/ph_enemy_bandit", new Vector2(-6, -0.3f));
            Character("Enemies/ph_enemy_archer", new Vector2(-3, -0.3f));
            Character("Enemies/ph_enemy_brute", new Vector2(0, -0.3f));
            Character("Enemies/ph_boss_bandit_chief", new Vector2(4, -0.05f), 0.6f, "Effects/ph_shadow_large");

            // 3줄: 이펙트 (보호막은 무당 검객에 씌워서 보여 줌)
            var fx = new Dictionary<string, float> {
                { "Effects/ph_fx_slash", -6.3f }, { "Effects/ph_fx_hit", -4.8f }, { "Effects/ph_fx_dagger", -3.6f },
                { "Effects/ph_fx_heal", -2.4f }, { "Effects/ph_fx_poison", -1.2f }, { "Effects/ph_fx_formation", 2.6f },
                { "Effects/ph_shadow", 5.8f } };
            foreach (var kv in fx) Place(kv.Key, new Vector2(kv.Value, -3.6f), 5);
            Character("Characters/ph_wudang_swordsman", new Vector2(0.4f, -3.3f));
            Place("Effects/ph_fx_shield", new Vector2(0.4f, -3.3f), 6);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[AssetPreview] 미리보기 씬 생성: {ScenePath}");
        }

        // 그림자 폭 = 캐릭터 폭의 절반 (32px -> ph_shadow 16px, 48px 보스 -> ph_shadow_large 24px)
        static void Character(string path, Vector2 pos, float feet = 0.4f, string shadow = "Effects/ph_shadow")
        {
            Place(path, pos, 0);
            Place(shadow, pos + new Vector2(0, -feet), -1);
        }

        static SpriteRenderer Place(string path, Vector2 pos, int order)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path + ".png");
            if (sprite == null)
            {
                Debug.LogWarning($"[AssetPreview] 스프라이트 없음: {path}");
                return null;
            }
            var go = new GameObject(System.IO.Path.GetFileName(path));
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
