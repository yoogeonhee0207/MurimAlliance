using System.Linq;
using MurimAlliance.Data;
using UnityEngine;

namespace MurimAlliance.Battle
{
    /// <summary>
    /// 전투 테스트용. 시작하면 아군을 왼쪽, 적을 오른쪽에 세우고 결과를 Console에 남긴다.
    /// </summary>
    public class BattleTestSpawner : MonoBehaviour
    {
        public UnitData[] allies;
        public UnitData[] enemies;
        public Sprite shadowNormal, shadowLarge;
        [Tooltip("아군·적 시작 위치(가운데로부터 칸)")] public float startX = 4f;
        [Tooltip("같은 편끼리 위아래 간격(칸)")] public float spacingY = 1.2f;

        bool finished;

        void Start()
        {
            Place(allies, Team.Ally, -startX);
            Place(enemies, Team.Enemy, startX);
            Debug.Log($"[Battle] 시작: 아군 {allies.Length}명 vs 적 {enemies.Length}명");
        }

        void Place(UnitData[] list, Team team, float x)
        {
            for (int i = 0; i < list.Length; i++)
            {
                float y = (i - (list.Length - 1) * 0.5f) * spacingY;
                UnitFactory.Spawn(list[i], team, new Vector2(x, y), shadowNormal, shadowLarge);
            }
        }

        void Update()
        {
            if (finished || Time.timeSinceLevelLoad < 0.5f) return;
            bool allyAlive = Unit.All.Any(u => u.IsAlive && u.Team == Team.Ally);
            bool enemyAlive = Unit.All.Any(u => u.IsAlive && u.Team == Team.Enemy);
            if (allyAlive && enemyAlive) return;
            finished = true;
            Debug.Log($"[Battle] 종료: {(allyAlive ? "아군 승리" : "적 승리")} ({Time.timeSinceLevelLoad:F1}초)");
        }
    }
}
