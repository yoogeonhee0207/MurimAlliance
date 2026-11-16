using UnityEngine;

namespace MurimAlliance.Data
{
    public enum Faction
    {
        [InspectorName("없음(적)")] None,
        [InspectorName("소림")] Shaolin,
        [InspectorName("무당")] Wudang,
        [InspectorName("화산")] Huashan,
        [InspectorName("당문")] Tang,
    }

    public enum Role
    {
        [InspectorName("없음(적)")] None,
        [InspectorName("권사")] Brawler,
        [InspectorName("검객")] Swordsman,
        [InspectorName("도사")] Taoist,
        [InspectorName("암기술사")] Assassin,
        [InspectorName("의원")] Healer,
    }

    public enum Personality
    {
        [InspectorName("없음")] None,
        [InspectorName("철벽형")] Ironwall,
        [InspectorName("나한형")] Arhat,
        [InspectorName("쾌검형")] SwiftSword,
        [InspectorName("매화형")] PlumBlossom,
        [InspectorName("반격형")] Counter,
    }

    public enum Grade
    {
        [InspectorName("인(人)")] In,
        [InspectorName("지(地)")] Ji,
        [InspectorName("천(天)")] Cheon,
        [InspectorName("신(神)")] Sin,
    }

    public enum AttackType
    {
        [InspectorName("근접 단일")] MeleeSingle,
        [InspectorName("근접 범위")] MeleeArea,
        [InspectorName("원거리")] Ranged,
        [InspectorName("원거리 + 중독")] RangedPoison,
        [InspectorName("회복")] Heal,
        [InspectorName("진법 범위 + 보호막")] FormationArea,
    }

    public enum ShadowSize
    {
        [InspectorName("일반(16px)")] Normal,
        [InspectorName("큰(24px)")] Large,
    }
}
