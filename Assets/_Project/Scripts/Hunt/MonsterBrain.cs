using MurimAlliance.Battle;
using UnityEngine;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 사냥터 몬스터의 판단.
    /// - 평소: 자기 자리(home) 근처를 천천히 어슬렁거림
    /// - 비선공: 맞으면 때린 상대를 쫓아가 반격 / 선공: 가까이 온 적(aggroRadius 안)을 먼저 공격
    /// - 자기 자리에서 leashRadius보다 멀어지면 쫓기를 포기하고 돌아감. 돌아가는 동안엔 반응하지 않고,
    ///   도착하면 체력이 다 찬다.
    /// </summary>
    public class MonsterBrain : UnitBrain
    {
        public bool aggressive;
        [Tooltip("선공 몬스터가 적을 알아채는 거리 (칸)")] public float aggroRadius = 3.5f;
        [Tooltip("자기 자리에서 이만큼 멀어지면 포기하고 돌아감 (칸)")] public float leashRadius = 6f;
        [Tooltip("어슬렁거리는 범위 (칸)")] public float wanderRadius = 1.5f;

        public Vector2 Home { get; set; }

        enum Mode { Idle, Chase, Return }
        Mode mode = Mode.Idle;
        Unit target;
        Vector2 wanderTo;
        float wanderTimer;
        bool wandering;

        void Start()
        {
            wanderTimer = Random.Range(1f, 4f);
        }

        public override void Tick(Unit unit, float dt)
        {
            if (mode == Mode.Return)
            {
                unit.MoveTo(Home);
                if ((unit.Position - Home).sqrMagnitude < 0.09f)
                {
                    mode = Mode.Idle;
                    unit.Stop();
                    unit.RestoreFullHp();
                    unit.ClearLastAttacker();
                }
                return;
            }

            // 대상 정하기: 맞았으면 때린 상대, 선공이면 가까운 적
            if (target != null && !target.IsAlive) target = null;
            if (target == null && unit.LastAttacker != null && unit.LastAttacker.IsAlive) target = unit.LastAttacker;
            if (target == null && aggressive) target = unit.FindNearestEnemy(aggroRadius);

            if (target != null)
            {
                mode = Mode.Chase;
                if ((unit.Position - Home).sqrMagnitude > leashRadius * leashRadius)
                {
                    mode = Mode.Return;
                    target = null;
                    unit.ClearLastAttacker();
                    return;
                }
                if (unit.InAttackRange(target)) unit.AttackTick(target, dt);
                else unit.MoveTo(target.Position);
                return;
            }

            // 대상이 없던 중에 쫓다가 놓쳤으면 자리로 돌아감
            if (mode == Mode.Chase) { mode = Mode.Return; return; }

            Wander(unit, dt);
        }

        void Wander(Unit unit, float dt)
        {
            wanderTimer -= dt;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(3f, 6f);
                wanderTo = Home + Random.insideUnitCircle * wanderRadius;
                wandering = true;
            }
            if (wandering && (unit.Position - wanderTo).sqrMagnitude > 0.04f)
                unit.MoveTo(wanderTo, unit.Data.moveSpeed * 0.5f);
            else
            {
                wandering = false;
                unit.Stop();
            }
        }
    }
}
