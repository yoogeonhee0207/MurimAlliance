using UnityEngine;

namespace MurimAlliance.Battle
{
    /// <summary>
    /// 시험장용 판단: 맵 어디에 있든 가장 가까운 적을 쫓아가 사거리에 들면 공격한다.
    /// (1대1 전투 테스트에서 쓰던 원래 행동)
    /// </summary>
    public class ArenaBrain : UnitBrain
    {
        Unit target;

        public override void Tick(Unit unit, float dt)
        {
            if (target == null || !target.IsAlive) target = unit.FindNearestEnemy();
            if (target == null) { unit.Stop(); return; }

            if (unit.InAttackRange(target)) unit.AttackTick(target, dt);
            else unit.MoveTo(target.Position);
        }
    }
}
