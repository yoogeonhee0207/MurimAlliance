using UnityEngine;

namespace MurimAlliance.Data
{
    /// <summary>
    /// 유닛 한 종류의 기본 데이터. 아군 제자와 적이 같은 구조를 쓴다(적은 문파 = 없음).
    /// 거리 단위는 "칸"(1칸 = 32px = 1 유닛).
    /// 레벨 성장, 경지, 환골탈태, 절기는 이후 단계에서 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnit", menuName = "천하무림맹/유닛 데이터")]
    public class UnitData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("화면에 보이는 이름")] public string displayName;
        public Faction faction;
        public Role role;
        public Personality personality;
        public Grade grade;
        public Sprite sprite;
        public ShadowSize shadowSize;
        [Tooltip("보스 여부")] public bool isBoss;

        [Header("전투 수치")]
        [Min(1)] public int maxHp = 100;
        [Min(0)] public int attack = 10;
        [Tooltip("받는 피해 = 공격력 × 100 ÷ (100 + 방어력). 100이면 피해 절반")][Min(0)] public int defense = 0;
        [Tooltip("몇 초마다 한 번 공격하는지")][Min(0.1f)] public float attackInterval = 1f;
        [Tooltip("몇 칸 앞까지 공격하는지")][Min(0.5f)] public float range = 1f;
        [Tooltip("1초에 몇 칸 움직이는지")][Min(0f)] public float moveSpeed = 1f;
        [Tooltip("0~1 (0.1 = 10%)")][Range(0f, 1f)] public float critChance = 0.05f;
        [Tooltip("치명타일 때 피해 배율")][Min(1f)] public float critMultiplier = 1.5f;

        [Header("공격 방식과 효과")]
        public AttackType attackType;
        [Tooltip("근접 범위·진법: 범위 반경(칸)")][Min(0f)] public float areaRadius;
        [Tooltip("중독: 1초당 피해")][Min(0)] public int poisonDamagePerSecond;
        [Tooltip("중독: 지속 시간(초)")][Min(0f)] public float poisonDuration;
        [Tooltip("의원: 1회 회복량")][Min(0)] public int healAmount;
        [Tooltip("도사: 아군 보호막량")][Min(0)] public int shieldAmount;
        [Tooltip("반격형: 맞았을 때 반격할 확률 0~1")][Range(0f, 1f)] public float counterChance;
    }
}
