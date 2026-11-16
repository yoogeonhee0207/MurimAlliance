using UnityEngine;

namespace MurimAlliance.Battle
{
    /// <summary>
    /// 유닛의 "판단". 매 프레임 Unit이 Tick을 부르면, 누구를 쫓을지·언제 공격할지 정해서
    /// unit.Move / MoveTo / Stop / AttackTick을 부른다.
    /// 종류: 시험장(ArenaBrain), 맹주(LeaderBrain), 몬스터·제자(이후 단계).
    /// </summary>
    public abstract class UnitBrain : MonoBehaviour
    {
        public abstract void Tick(Unit unit, float dt);
    }
}
