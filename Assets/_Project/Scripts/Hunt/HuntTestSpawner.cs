using MurimAlliance.Battle;
using MurimAlliance.Data;
using UnityEngine;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 사냥터 시험용 생성기. 게임이 시작하면 맹주를 입구에 세우고 카메라가 따라가게 한다.
    /// 시험용으로 가만히 서 있는 적(판단 없음)도 세울 수 있다. (몬스터 생성은 5단계에서 구역별로)
    /// Unit의 내부 값은 씬에 저장되지 않으므로 유닛은 항상 실행 중에 만든다.
    /// </summary>
    public class HuntTestSpawner : MonoBehaviour
    {
        public UnitData leaderData;
        public Sprite shadowNormal, shadowLarge;
        public CameraFollow cameraFollow;
        public AutoHuntButton autoHuntButton;
        public AttackButton attackButton;

        [Header("시험용 적 (가만히 서 있음)")]
        public UnitData dummyEnemy;
        public Vector2[] dummyPositions;

        public Unit Leader { get; private set; }

        void Start()
        {
            var map = HuntMap.Current;
            Vector2 spawn = map != null && map.spawnPoint != null ? (Vector2)map.spawnPoint.position : Vector2.zero;

            Leader = UnitFactory.Spawn(leaderData, Team.Ally, spawn, shadowNormal, shadowLarge);
            Leader.name = "Leader";
            Leader.destroyOnDeath = false;
            var brain = Leader.gameObject.AddComponent<LeaderBrain>();
            if (cameraFollow != null) cameraFollow.target = Leader.transform;
            if (autoHuntButton != null) autoHuntButton.Bind(brain);
            if (attackButton != null) attackButton.Bind(brain);

            if (dummyEnemy != null && dummyPositions != null)
                foreach (var p in dummyPositions)
                    UnitFactory.Spawn(dummyEnemy, Team.Enemy, p, shadowNormal, shadowLarge);

            Debug.Log($"[HuntTest] 맹주 생성: {leaderData.displayName} {spawn}, 시험용 적 {dummyPositions?.Length ?? 0}명");
        }
    }
}
