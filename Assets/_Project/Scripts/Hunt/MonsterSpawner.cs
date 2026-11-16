using System.Collections.Generic;
using MurimAlliance.Battle;
using UnityEngine;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 구역별 몬스터 생성. 시작할 때 각 구역을 최대 마릿수까지 채우고,
    /// 잡히면 respawnDelay초 뒤 같은 구역의 "화면 밖" 무작위 위치에 다시 생성한다.
    /// 나무·바위·다른 유닛과 겹치는 곳에는 생성하지 않고, 맵 전체 마릿수는 globalCap을 넘지 않는다.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        public Sprite shadowNormal, shadowLarge;
        [Tooltip("맵 전체 몬스터 상한")] public int globalCap = 30;
        [Tooltip("잡힌 뒤 다시 생길 때까지 (초)")] public float respawnDelay = 5f;
        [Tooltip("처음 채울 때 맹주 시작 위치에서 이만큼은 비움 (칸)")] public float clearAroundSpawn = 4f;

        class ZoneState
        {
            public HuntZone zone;
            public readonly List<Unit> alive = new List<Unit>();
            public readonly List<float> respawnAt = new List<float>();
        }

        readonly List<ZoneState> states = new List<ZoneState>();
        HuntMap map;

        public int TotalAlive
        {
            get { int n = 0; foreach (var s in states) n += s.alive.Count; return n; }
        }

        void Start()
        {
            map = HuntMap.Current;
            if (map == null) { Debug.LogError("[Hunt] HuntMap이 없어 몬스터를 만들 수 없습니다."); return; }

            foreach (var z in map.zones)
            {
                if (z == null || z.monsters == null || z.monsters.Length == 0) continue;
                var st = new ZoneState { zone = z };
                states.Add(st);
                for (int i = 0; i < z.maxMonsters && TotalAlive < globalCap; i++)
                    TrySpawn(st, false);
            }
            Debug.Log($"[Hunt] 몬스터 생성 완료: {Summary()}");
        }

        void Update()
        {
            float now = Time.time;
            foreach (var st in states)
            {
                for (int i = st.respawnAt.Count - 1; i >= 0; i--)
                {
                    if (now < st.respawnAt[i]) continue;
                    if (st.alive.Count >= st.zone.maxMonsters) { st.respawnAt.RemoveAt(i); continue; }
                    if (TotalAlive >= globalCap) break;
                    if (TrySpawn(st, true)) st.respawnAt.RemoveAt(i);
                    else st.respawnAt[i] = now + 0.5f;   // 화면 밖 빈자리가 없으면 잠시 뒤 다시
                }
            }
        }

        bool TrySpawn(ZoneState st, bool offscreen)
        {
            Vector2 spawn = map.spawnPoint != null ? (Vector2)map.spawnPoint.position : Vector2.zero;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector2 p = st.zone.RandomPoint(1.5f);
                if (!offscreen && (p - spawn).sqrMagnitude < clearAroundSpawn * clearAroundSpawn) continue;
                if (offscreen && OnScreen(p)) continue;
                if (Physics2D.OverlapCircle(p + new Vector2(0f, 0.1f), 0.45f) != null) continue;   // 나무·바위·유닛과 겹침

                var data = st.zone.monsters[Random.Range(0, st.zone.monsters.Length)];
                var unit = UnitFactory.Spawn(data, Team.Enemy, p, shadowNormal, shadowLarge);
                var brain = unit.gameObject.AddComponent<MonsterBrain>();
                brain.Home = p;
                brain.aggressive = st.zone.aggressive;
                unit.Died += u => OnMonsterDied(st, u);
                st.alive.Add(unit);
                return true;
            }
            return false;
        }

        void OnMonsterDied(ZoneState st, Unit u)
        {
            st.alive.Remove(u);
            st.respawnAt.Add(Time.time + respawnDelay);
        }

        /// <summary>카메라에 보이는 범위(+1칸 여유) 안인가.</summary>
        static bool OnScreen(Vector2 p)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            float halfH = cam.orthographicSize + 1f, halfW = cam.orthographicSize * cam.aspect + 1f;
            Vector2 c = cam.transform.position;
            return Mathf.Abs(p.x - c.x) < halfW && Mathf.Abs(p.y - c.y) < halfH;
        }

        public string Summary()
        {
            var parts = new List<string>();
            foreach (var st in states) parts.Add($"{st.zone.zoneName} {st.alive.Count}/{st.zone.maxMonsters}");
            return $"{string.Join(", ", parts)} (전체 {TotalAlive}/{globalCap})";
        }
    }
}
