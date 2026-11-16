using System.Collections.Generic;
using System.IO;
using MurimAlliance.Hunt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// 시험용 사냥터 맵 씬을 만든다 (45x25칸, 구역 3개: 입구·숲길·깊은 숲).
    /// 메뉴: Tools > 천하무림맹 > 사냥터 테스트 씬
    ///       Tools > 천하무림맹 > 사냥터 전체 이미지 저장 (Temp/HuntMapPreview.png)
    /// </summary>
    public static class HuntTestSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/HuntTest.unity";
        const string Art = "Assets/_Project/Art/";
        const string TileDir = "Assets/_Project/Art/Tiles/";
        const string PreviewPath = "Temp/HuntMapPreview.png";

        const int MapW = 45, MapH = 25, ZoneW = 15;
        const int PathY = 12;                       // 가로로 지나가는 흙길 (y=12~13)
        static readonly Vector2 Spawn = new Vector2(3f, 12.5f);
        const int Seed = 20261116;

        [MenuItem("Tools/천하무림맹/사냥터 테스트 씬")]
        public static void CreateHuntScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[HuntTest] 플레이 중에는 씬을 만들 수 없습니다. 플레이를 멈춘 뒤 다시 실행하세요.");
                return;
            }
            // 주의: 타일은 새 씬을 만든 "뒤에" 불러온다. 먼저 불러오면 NewScene이 쓰지 않는 에셋을
            // 메모리에서 내리면서 참조가 비어 SetTile이 아무것도 깔지 않는다.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var dirt = GetTile("ph_tile_dirt");
            var grass = GetTile("ph_tile_grass");
            var dark = GetTile("ph_tile_grass_dark");
            if (dirt == null || grass == null || dark == null) return;

            // 카메라: 480x270, 32 PPU. 입구 쪽을 비춤 (따라가기는 2단계에서)
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / 2f / 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(20, 24, 22, 255);
            camGo.transform.position = new Vector3(7.5f, 12.5f, -10);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 32; ppc.refResolutionX = 480; ppc.refResolutionY = 270;

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;

            // 바닥 타일맵
            var grid = new GameObject("Grid").AddComponent<Grid>();
            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(grid.transform);
            var ground = groundGo.AddComponent<Tilemap>();
            var groundRenderer = groundGo.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = -32000;   // 유닛(-y*100)보다 항상 뒤

            var rnd = new System.Random(Seed);
            PaintGround(ground, dirt, grass, dark, rnd);

            // 맵 정보와 구역
            var mapGo = new GameObject("HuntMap");
            var map = mapGo.AddComponent<HuntMap>();
            map.bounds = new Rect(0, 0, MapW, MapH);
            var spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.SetParent(mapGo.transform);
            spawnGo.transform.position = Spawn;
            map.spawnPoint = spawnGo.transform;
            // 구역별 몬스터: 입구·숲길은 비선공, 깊은 숲은 선공 (원거리는 공격 방식 구현 뒤에 추가)
            var bandit = LoadUnit("enemy_bandit");
            var brute = LoadUnit("enemy_brute");
            map.zones.Add(Zone(mapGo.transform, "입구", 0, new Color(0.9f, 0.75f, 0.4f), new[] { bandit }, 6, false));
            map.zones.Add(Zone(mapGo.transform, "숲길", 1, new Color(0.5f, 0.9f, 0.4f), new[] { bandit, brute }, 8, false));
            map.zones.Add(Zone(mapGo.transform, "깊은 숲", 2, new Color(0.3f, 0.6f, 0.9f), new[] { brute }, 8, true));

            // 장애물: 가장자리 숲(두께 1~3겹, 들쭉날쭉) + 안쪽에 흩어진 나무·바위
            var tree = LoadSprite("Backgrounds/ph_tree");
            var rock = LoadSprite("Backgrounds/ph_rock");
            var obstacles = new GameObject("Obstacles").transform;
            var placed = new List<Vector2>();
            ForestBorder(obstacles, tree, rnd, placed);

            // 나무 사이 틈으로 맵 밖에 나가지 않게 보이지 않는 테두리 벽
            var wall = mapGo.AddComponent<EdgeCollider2D>();
            wall.points = new[] {
                new Vector2(0.5f, 0.5f), new Vector2(MapW - 0.5f, 0.5f),
                new Vector2(MapW - 0.5f, MapH - 0.5f), new Vector2(0.5f, MapH - 0.5f),
                new Vector2(0.5f, 0.5f) };

            int[] treesPerZone = { 6, 14, 22 };     // 깊을수록 빽빽하게
            int[] rocksPerZone = { 6, 5, 4 };
            for (int z = 0; z < 3; z++)
            {
                Scatter(obstacles, tree, z, treesPerZone[z], true, rnd, placed);
                Scatter(obstacles, rock, z, rocksPerZone[z], false, rnd, placed);
            }

            // 맹주는 실행 중에 생성기가 만든다 (Unit 내부 값은 씬에 저장되지 않음). 카메라는 맹주를 따라감.
            var follow = camGo.AddComponent<CameraFollow>();
            var spawner = new GameObject("HuntTestSpawner").AddComponent<HuntTestSpawner>();
            spawner.leaderData = LoadUnit("ally_wudang_swordsman");
            spawner.shadowNormal = LoadSprite("Effects/ph_shadow");
            spawner.shadowLarge = LoadSprite("Effects/ph_shadow_large");
            spawner.cameraFollow = follow;

            // 구역별 몬스터 생성기
            var monsterSpawner = mapGo.AddComponent<MonsterSpawner>();
            monsterSpawner.shadowNormal = spawner.shadowNormal;
            monsterSpawner.shadowLarge = spawner.shadowLarge;

            // 실행 전 화면도 맵 밖이 보이지 않게, 첫 프레임과 같은 위치(맵 경계 안)로 둔다
            float halfW = 480f / 32f / 2f, halfH = 270f / 32f / 2f;
            camGo.transform.position = new Vector3(
                Mathf.Clamp(Spawn.x, halfW, MapW - halfW),
                Mathf.Clamp(Spawn.y + follow.yOffset, halfH, MapH - halfH), -10);
            var ui = CreateJoystickUI();
            spawner.attackButton = CreateAttackButton(ui);
            spawner.autoHuntButton = CreateAutoHuntButton(ui);

            EditorSceneManager.SaveScene(scene, ScenePath);
            int tiles = ground.GetUsedTilesCount() > 0 ? ground.GetTilesBlock(ground.cellBounds).Length : 0;
            Debug.Log($"[HuntTest] 사냥터 테스트 씬 생성: {ScenePath} (맵 {MapW}x{MapH}칸, 바닥 타일 {tiles}칸, 장애물 {obstacles.childCount}개)");
        }

        /// <summary>맵 전체를 1칸=32px 그대로 PNG로 저장한다 (확인용).</summary>
        [MenuItem("Tools/천하무림맹/사냥터 전체 이미지 저장")]
        public static void SaveMapPreview()
        {
            var map = Object.FindAnyObjectByType<HuntMap>();
            if (map == null)
            {
                Debug.LogError("[HuntTest] 열린 씬에 HuntMap이 없습니다. 먼저 사냥터 테스트 씬을 만드세요.");
                return;
            }
            var b = map.bounds;
            int w = Mathf.RoundToInt(b.width * 32), h = Mathf.RoundToInt(b.height * 32);

            var camGo = new GameObject("_PreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = b.height / 2f;
            cam.aspect = b.width / b.height;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(b.center.x, b.center.y, -10);

            var rt = new RenderTexture(w, h, 24) { filterMode = FilterMode.Point };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            File.WriteAllBytes(PreviewPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
            Debug.Log($"[HuntTest] 맵 전체 이미지 저장: {PreviewPath} ({w}x{h})");
        }

        // ---------- 가장자리 숲 ----------

        /// <summary>
        /// 맵 가장자리에 숲을 두른다. 변마다 두께(1~3겹)가 자리에 따라 달라지고,
        /// 나무 위치는 격자에서 조금씩 어긋난다. 바깥 겹은 촘촘하게, 안쪽 겹은 듬성듬성.
        /// </summary>
        static void ForestBorder(Transform parent, Sprite tree, System.Random rnd, List<Vector2> placed)
        {
            int[] top = Depths(rnd, MapW), bottom = Depths(rnd, MapW);
            int[] left = Depths(rnd, MapH), right = Depths(rnd, MapH);

            for (int y = 0; y < MapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                // 가장 가까운 변까지의 거리와 그 자리의 숲 두께
                int dl = x, dr = MapW - 1 - x, db = y, dt = MapH - 1 - y;
                int d = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
                int depth = d == dl ? left[y] : d == dr ? right[y] : d == db ? bottom[x] : top[x];
                if (d >= depth) continue;

                double chance = d == 0 ? 0.95 : d == 1 ? 0.6 : 0.35;
                if (rnd.NextDouble() > chance) continue;

                var p = new Vector2(
                    x + 0.5f + ((float)rnd.NextDouble() - 0.5f) * 0.5f,
                    y + 0.3f + ((float)rnd.NextDouble() - 0.5f) * 0.5f);
                if (Vector2.Distance(p, Spawn) < 2.5f) continue;   // 시작 위치 주변은 비움
                placed.Add(p);
                Obstacle(parent, tree, p, true);
            }
        }

        /// <summary>변을 따라 숲 두께를 1~3 사이에서 천천히 바꾼다.</summary>
        static int[] Depths(System.Random rnd, int length)
        {
            var d = new int[length];
            int cur = rnd.Next(1, 4);
            for (int i = 0; i < length; i++)
            {
                if (rnd.NextDouble() < 0.3) cur = Mathf.Clamp(cur + (rnd.Next(0, 2) == 0 ? -1 : 1), 1, 3);
                d[i] = cur;
            }
            return d;
        }

        // ---------- 바닥 ----------

        /// <summary>
        /// 바닥을 칠한다. 보이는 구역 경계는 구불구불하게(좌우 최대 2칸) 하고,
        /// 구역마다 다른 바닥 덩어리를 섞어 단조롭지 않게 한다.
        /// (구역 판정용 HuntZone 영역은 반듯한 사각형 그대로)
        /// </summary>
        static void PaintGround(Tilemap ground, TileBase dirt, TileBase grass, TileBase dark, System.Random rnd)
        {
            int[] edge1 = Wobble(rnd, ZoneW), edge2 = Wobble(rnd, ZoneW * 2);
            var map = new TileBase[MapW, MapH];

            // 1) 구역 바탕 (경계 바로 옆 한 칸은 30% 확률로 이웃 바닥과 섞음)
            for (int y = 0; y < MapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                int zone = x < edge1[y] ? 0 : x < edge2[y] ? 1 : 2;
                int d1 = x - edge1[y], d2 = x - edge2[y];
                if ((d1 == -1 || d1 == 0) && rnd.NextDouble() < 0.3) zone = d1 == -1 ? 1 : 0;
                else if ((d2 == -1 || d2 == 0) && rnd.NextDouble() < 0.3) zone = d2 == -1 ? 2 : 1;
                map[x, y] = zone == 0 ? dirt : zone == 1 ? grass : dark;
            }

            // 2) 바닥 덩어리: 입구엔 풀밭, 숲길엔 흙·짙은 풀, 깊은 숲엔 풀
            Patches(map, rnd, grass, 2, ZoneW - 3, 4, 1.2f, 2.2f);
            Patches(map, rnd, dark, ZoneW + 2, ZoneW * 2 - 3, 3, 1.2f, 2f);
            Patches(map, rnd, dirt, ZoneW + 1, ZoneW * 2 - 2, 2, 1f, 2f);
            Patches(map, rnd, grass, ZoneW * 2 + 2, MapW - 3, 3, 1.2f, 2f);

            // 3) 흙길: 숲길부터 오른쪽 끝까지, 위아래로 1칸씩 굽음
            for (int x = ZoneW - 2; x < MapW; x++)
            {
                int off = Mathf.RoundToInt(Mathf.Sin(x * 0.28f));
                map[x, PathY + off] = dirt;
                map[x, PathY + 1 + off] = dirt;
            }

            for (int y = 0; y < MapH; y++)
            for (int x = 0; x < MapW; x++)
                ground.SetTile(new Vector3Int(x, y, 0), map[x, y]);
        }

        /// <summary>세로 경계선의 x 위치를 줄마다 조금씩 흔든다 (±2칸, 이웃 줄과 1칸 이내 차이).</summary>
        static int[] Wobble(System.Random rnd, int baseX)
        {
            var e = new int[MapH];
            int off = 0;
            for (int y = 0; y < MapH; y++)
            {
                if (rnd.NextDouble() < 0.45) off += rnd.Next(0, 2) == 0 ? -1 : 1;
                off = Mathf.Clamp(off, -2, 2);
                e[y] = baseX + off;
            }
            return e;
        }

        /// <summary>x 범위 안에 타원형 바닥 덩어리를 count개 칠한다 (가장자리는 울퉁불퉁).</summary>
        static void Patches(TileBase[,] map, System.Random rnd, TileBase tile, int xMin, int xMax, int count, float rMin, float rMax)
        {
            for (int i = 0; i < count; i++)
            {
                float cx = xMin + (float)rnd.NextDouble() * (xMax - xMin);
                float cy = 2f + (float)rnd.NextDouble() * (MapH - 4f);
                float rx = rMin + (float)rnd.NextDouble() * (rMax - rMin);
                float ry = rx * (0.6f + (float)rnd.NextDouble() * 0.4f);
                for (int y = Mathf.FloorToInt(cy - ry - 1); y <= cy + ry + 1; y++)
                for (int x = Mathf.FloorToInt(cx - rx - 1); x <= cx + rx + 1; x++)
                {
                    if (x < 1 || y < 1 || x >= MapW - 1 || y >= MapH - 1) continue;
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    float d = dx * dx + dy * dy;
                    if (d < 0.7f || (d < 1.15f && rnd.NextDouble() < 0.5)) map[x, y] = tile;
                }
            }
        }

        // ---------- 플레이 중 테스트 (에디터 전용) ----------

        [MenuItem("Tools/천하무림맹/테스트/맹주 오른쪽 이동 (4초)")]
        static void TestMoveRight() => TestMove(Vector2.right, 4f);

        [MenuItem("Tools/천하무림맹/테스트/맹주 왼쪽 이동 (4초)")]
        static void TestMoveLeft() => TestMove(Vector2.left, 4f);

        [MenuItem("Tools/천하무림맹/테스트/맹주 위로 이동 (4초)")]
        static void TestMoveUp() => TestMove(Vector2.up, 4f);

        [MenuItem("Tools/천하무림맹/테스트/맹주 아래로 이동 (4초)")]
        static void TestMoveDown() => TestMove(Vector2.down, 4f);

        static void TestMove(Vector2 dir, float seconds)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[HuntTest] 플레이 중에만 쓸 수 있습니다.");
                return;
            }
            LeaderBrain.DebugInput = dir;
            LeaderBrain.DebugUntil = Time.time + seconds;
            var leader = Object.FindAnyObjectByType<LeaderBrain>();
            Debug.Log($"[HuntTest] 테스트 이동 {dir} {seconds}초 시작, 맹주 위치 {(leader != null ? (Vector2)leader.transform.position : Vector2.zero)}");
        }

        [MenuItem("Tools/천하무림맹/테스트/자동사냥 켜기·끄기")]
        static void TestToggleAuto()
        {
            var brain = Object.FindAnyObjectByType<LeaderBrain>();
            if (brain != null) brain.ToggleAutoHunt();
        }

        [MenuItem("Tools/천하무림맹/테스트/공격 버튼 한 번")]
        static void TestAttackOnce()
        {
            var brain = Object.FindAnyObjectByType<LeaderBrain>();
            if (brain != null) brain.RequestAttack();
        }

        [MenuItem("Tools/천하무림맹/테스트/맹주를 가까운 적 옆으로")]
        static void TestTeleportNearEnemy()
        {
            var brain = Object.FindAnyObjectByType<LeaderBrain>();
            var unit = brain != null ? brain.GetComponent<MurimAlliance.Battle.Unit>() : null;
            var enemy = unit != null ? unit.FindNearestEnemy() : null;
            if (enemy == null) return;
            Vector2 p = enemy.Position + new Vector2(-0.8f, 0f);
            unit.transform.position = p;
            var rb = unit.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = p;
            Debug.Log($"[HuntTest] 맹주를 {enemy.Data.displayName} 옆 {p}로 옮김");
        }

        static double spamStart;
        static int spamPresses, spamHitsBefore;

        [MenuItem("Tools/천하무림맹/테스트/공격 버튼 연타 (1.2초에 8번)")]
        static void TestSpamAttack()
        {
            var brain = Object.FindAnyObjectByType<LeaderBrain>();
            if (brain == null) return;
            spamStart = EditorApplication.timeSinceStartup;
            spamPresses = 0;
            spamHitsBefore = brain.HitCount;
            EditorApplication.update -= SpamUpdate;
            EditorApplication.update += SpamUpdate;
        }

        static void SpamUpdate()
        {
            var brain = Object.FindAnyObjectByType<LeaderBrain>();
            double t = EditorApplication.timeSinceStartup - spamStart;
            if (brain == null || !Application.isPlaying) { EditorApplication.update -= SpamUpdate; return; }
            while (spamPresses < 8 && t >= spamPresses * 0.15)
            {
                brain.RequestAttack();
                spamPresses++;
            }
            if (t >= 1.6)   // 마지막 누름(1.05초) 뒤 여유를 두고 집계
            {
                EditorApplication.update -= SpamUpdate;
                Debug.Log($"[HuntTest] 공격 버튼 8번 연타(1.2초) → 실제 공격 {brain.HitCount - spamHitsBefore}번");
            }
        }

        [MenuItem("Tools/천하무림맹/테스트/맹주 쓰러뜨리기")]
        static void TestKillLeader()
        {
            var brain = Object.FindAnyObjectByType<LeaderBrain>();
            var unit = brain != null ? brain.GetComponent<MurimAlliance.Battle.Unit>() : null;
            if (unit != null && unit.IsAlive) unit.TakeDamage(99999, false);
        }

        [MenuItem("Tools/천하무림맹/테스트/몬스터 수 출력")]
        static void TestPrintMonsters()
        {
            var ms = Object.FindAnyObjectByType<MonsterSpawner>();
            if (ms != null) Debug.Log($"[Hunt] 몬스터: {ms.Summary()}");
        }

        [MenuItem("Tools/천하무림맹/테스트/맹주 위치 출력")]
        static void TestPrintPosition()
        {
            var leader = Object.FindAnyObjectByType<LeaderBrain>();
            var cam = Camera.main;
            if (leader == null) return;
            Debug.Log($"[HuntTest] 맹주 {(Vector2)leader.transform.position}, 카메라 {(cam != null ? (Vector2)cam.transform.position : Vector2.zero)}");
        }

        // ---------- 맹주·조이스틱 ----------

        /// <summary>화면 UI: 왼쪽 절반 전체가 플로팅 조이스틱 영역. UI는 실제 화면 해상도로 그린다.</summary>
        static Transform CreateJoystickUI()
        {
            var canvasGo = new GameObject("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 조이스틱 영역 (보이지 않는 터치 판)
            var zone = new GameObject("JoystickZone", typeof(RectTransform)).GetComponent<RectTransform>();
            zone.SetParent(canvasGo.transform, false);
            zone.anchorMin = new Vector2(0, 0); zone.anchorMax = new Vector2(0.5f, 1f);
            zone.pivot = Vector2.zero;
            zone.offsetMin = zone.offsetMax = Vector2.zero;
            var zoneImg = zone.gameObject.AddComponent<Image>();
            zoneImg.color = new Color(0, 0, 0, 0);   // 투명하지만 터치는 받음

            var baseRect = UIImage("Base", zone, 240);
            baseRect.anchorMin = baseRect.anchorMax = Vector2.zero;
            var fill = UIImage("Fill", baseRect, 228);
            fill.SetAsFirstSibling();
            var knob = UIImage("Knob", baseRect, 100);

            var js = zone.gameObject.AddComponent<VirtualJoystick>();
            js.baseRect = baseRect;
            js.knobRect = knob;
            js.fillRect = fill;
            baseRect.gameObject.SetActive(false);   // 누르기 전엔 숨김 (편집 화면에서도)

            // 새 입력 시스템용 이벤트 시스템
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            return canvasGo.transform;
        }

        // 오른쪽 아래 배치: 공격 버튼(240) 모서리, 자동 버튼(140)은 그 왼쪽 위
        const float EdgeMargin = 50f, AttackSize = 240f, AutoSize = 140f;

        /// <summary>오른쪽 아래 모서리의 큰 공격 버튼.</summary>
        static AttackButton CreateAttackButton(Transform canvas)
        {
            var rt = new GameObject("AttackButton", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-EdgeMargin, EdgeMargin);
            rt.sizeDelta = new Vector2(AttackSize, AttackSize);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.55f, 0.16f, 0.14f, 0.9f);
            var text = UIText(rt, "공격", 64);

            var btn = rt.gameObject.AddComponent<AttackButton>();
            btn.background = bg;
            return btn;
        }

        static Text UIText(RectTransform parent, string content, int size)
        {
            var textRt = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            textRt.SetParent(parent, false);
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            var text = textRt.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // 임시 글꼴 (한글은 기기 글꼴로 대체 표시)
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.color = new Color(0.95f, 0.92f, 0.85f);
            text.text = content;
            return text;
        }

        /// <summary>자동사냥 버튼: 공격 버튼의 왼쪽 위, 조금 작게.</summary>
        static AutoHuntButton CreateAutoHuntButton(Transform canvas)
        {
            var rt = new GameObject("AutoHuntButton", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-(EdgeMargin + AttackSize + 20f), EdgeMargin + AttackSize - AutoSize * 0.5f);
            rt.sizeDelta = new Vector2(AutoSize, AutoSize);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.14f, 0.75f);   // 꺼짐 색 (실행 전 화면도 같게)
            rt.gameObject.AddComponent<Button>().targetGraphic = bg;

            var textRt = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            textRt.SetParent(rt, false);
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            var text = textRt.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // 임시 글꼴 (한글은 기기 글꼴로 대체 표시)
            text.fontSize = 36;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.text = "자동\nOFF";
            text.color = new Color(0.85f, 0.85f, 0.8f);

            var btn = rt.gameObject.AddComponent<AutoHuntButton>();
            btn.background = bg;
            btn.label = text;
            return btn;
        }

        // 원 그림은 VirtualJoystick이 실행 시작 때 만든다 (에디터에서 만든 그림은 씬에 저장되지 않음)
        static RectTransform UIImage(string name, Transform parent, float size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return rt;
        }

        // ---------- 도우미 ----------

        static int Order(float feetY) => Mathf.RoundToInt(-feetY * 100f);

        static HuntZone Zone(Transform parent, string name, int index, Color color,
                             MurimAlliance.Data.UnitData[] monsters, int max, bool aggressive)
        {
            var go = new GameObject("Zone_" + name);
            go.transform.SetParent(parent);
            var z = go.AddComponent<HuntZone>();
            z.zoneName = name;
            z.area = new Rect(index * ZoneW, 0, ZoneW, MapH);
            z.gizmoColor = color;
            z.monsters = monsters;
            z.maxMonsters = max;
            z.aggressive = aggressive;
            return z;
        }

        static void Scatter(Transform parent, Sprite sprite, int zone, int count, bool isTree,
                            System.Random rnd, List<Vector2> placed)
        {
            int tries = 0;
            while (count > 0 && tries++ < 500)
            {
                var p = new Vector2(
                    zone * ZoneW + 2f + (float)rnd.NextDouble() * (ZoneW - 4f),
                    2f + (float)rnd.NextDouble() * (MapH - 4.5f));
                if (Vector2.Distance(p, Spawn) < 3f) continue;                 // 시작 위치 비우기
                if (p.y > PathY - 2f && p.y < PathY + 3.5f) continue;          // 흙길(굽은 폭 포함) 비우기
                bool tooClose = false;
                foreach (var q in placed)
                    if (Vector2.Distance(p, q) < 1.8f) { tooClose = true; break; }
                if (tooClose) continue;
                placed.Add(p);
                Obstacle(parent, sprite, p, isTree);
                count--;
            }
        }

        /// <summary>장애물 하나. 루트 = 발밑 위치, 그림은 위로 올려 붙이고, 충돌은 밑동에만.</summary>
        static void Obstacle(Transform parent, Sprite sprite, Vector2 feet, bool isTree)
        {
            if (sprite == null) return;
            var go = new GameObject(isTree ? "Tree" : "Rock");
            go.transform.SetParent(parent);
            go.transform.position = feet;

            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(go.transform, false);
            float halfH = sprite.bounds.size.y / 2f;
            body.transform.localPosition = new Vector3(0, halfH - (isTree ? 0.1f : 0.15f), 0);
            body.sprite = sprite;
            body.sortingOrder = Order(feet.y);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = isTree ? new Vector2(0.4f, 0.3f) : new Vector2(0.65f, 0.3f);
            col.offset = new Vector2(0, 0.1f);
        }

        static Tile GetTile(string name)
        {
            string spritePath = Art + "Backgrounds/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogError($"[HuntTest] 타일 그림 없음: {spritePath}");
                return null;
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Tiles"))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Tiles");

            string tilePath = TileDir + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        static MurimAlliance.Data.UnitData LoadUnit(string name)
        {
            var d = AssetDatabase.LoadAssetAtPath<MurimAlliance.Data.UnitData>($"Assets/_Project/Data/Units/{name}.asset");
            if (d == null) Debug.LogError($"[HuntTest] 유닛 데이터 없음: {name}");
            return d;
        }

        static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path + ".png");
            if (s == null) Debug.LogWarning($"[HuntTest] 스프라이트 없음: {path}");
            return s;
        }
    }
}
