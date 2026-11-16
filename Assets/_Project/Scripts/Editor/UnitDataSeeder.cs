using System.IO;
using MurimAlliance.Data;
using UnityEditor;
using UnityEngine;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// 임시 유닛 데이터(아군 6, 적 4)를 만든다. 이미 있는 파일은 덮어쓰지 않는다.
    /// 메뉴: Tools > 천하무림맹 > 임시 유닛 데이터 생성
    /// 수치는 전투 시제품용 임시값이며 밸런스 작업 때 다시 정한다.
    /// </summary>
    public static class UnitDataSeeder
    {
        const string Folder = "Assets/_Project/Data/Units";
        const string Art = "Assets/_Project/Art/";

        [MenuItem("Tools/천하무림맹/임시 유닛 데이터 생성")]
        public static void Seed()
        {
            int made = 0;
            // 아군 (이름, 파일, 문파, 역할, 성격, 등급, 그림, 체력, 공격, 방어, 간격, 사거리, 이동, 치명, 공격방식)
            made += Make("ally_shaolin_brawler", "소림 권사", Faction.Shaolin, Role.Brawler, Personality.Ironwall, Grade.Ji,
                "Characters/ph_shaolin_brawler", 300, 12, 10, 1.2f, 1f, 1.2f, 0.05f, AttackType.MeleeSingle);
            made += Make("ally_huashan_swordsman", "화산 검객", Faction.Huashan, Role.Swordsman, Personality.SwiftSword, Grade.Ji,
                "Characters/ph_huashan_swordsman", 160, 28, 4, 0.9f, 1f, 1.6f, 0.15f, AttackType.MeleeSingle, critMul: 1.8f);
            made += Make("ally_wudang_swordsman", "무당 검객", Faction.Wudang, Role.Swordsman, Personality.None, Grade.Ji,
                "Characters/ph_wudang_swordsman", 180, 22, 6, 1.0f, 1f, 1.5f, 0.10f, AttackType.MeleeSingle);
            made += Make("ally_wudang_taoist", "무당 도사", Faction.Wudang, Role.Taoist, Personality.None, Grade.Ji,
                "Characters/ph_wudang_taoist", 140, 14, 3, 1.8f, 3.5f, 1.0f, 0.05f, AttackType.FormationArea,
                area: 1.5f, shield: 40);
            made += Make("ally_tang_assassin", "당문 암기술사", Faction.Tang, Role.Assassin, Personality.None, Grade.Ji,
                "Characters/ph_tang_assassin", 120, 8, 2, 1.0f, 4.5f, 1.1f, 0.10f, AttackType.RangedPoison,
                poisonDps: 6, poisonDur: 4f);
            made += Make("ally_tang_healer", "당문 의원", Faction.Tang, Role.Healer, Personality.None, Grade.Ji,
                "Characters/ph_tang_healer", 130, 6, 3, 1.5f, 4f, 1.0f, 0f, AttackType.Heal, heal: 30);

            // 적
            made += Make("enemy_bandit", "산적", Faction.None, Role.None, Personality.None, Grade.In,
                "Enemies/ph_enemy_bandit", 120, 10, 2, 1.2f, 1f, 1.3f, 0.05f, AttackType.MeleeSingle);
            made += Make("enemy_archer", "산적 궁수", Faction.None, Role.None, Personality.None, Grade.In,
                "Enemies/ph_enemy_archer", 80, 9, 1, 1.5f, 4.5f, 1.2f, 0.05f, AttackType.Ranged);
            made += Make("enemy_brute", "거한", Faction.None, Role.None, Personality.None, Grade.In,
                "Enemies/ph_enemy_brute", 260, 14, 8, 1.6f, 1f, 0.9f, 0.05f, AttackType.MeleeSingle);
            made += Make("boss_bandit_chief", "녹림 두목", Faction.None, Role.None, Personality.None, Grade.In,
                "Enemies/ph_boss_bandit_chief", 1500, 30, 10, 1.4f, 1.2f, 1.0f, 0.10f, AttackType.MeleeArea,
                area: 1.2f, boss: true);

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnitData>($"{Folder}/ally_shaolin_brawler.asset");
            Debug.Log($"[UnitDataSeeder] 새로 만든 유닛 데이터 {made}개 (이미 있던 파일은 그대로 둠)");
        }

        static int Make(string file, string name, Faction faction, Role role, Personality personality, Grade grade,
            string spritePath, int hp, int atk, int def, float interval, float range, float move, float crit,
            AttackType type, float critMul = 1.5f, float area = 0f, int shield = 0, int poisonDps = 0,
            float poisonDur = 0f, int heal = 0, float counter = 0f, bool boss = false)
        {
            var path = $"{Folder}/{file}.asset";
            if (File.Exists(path)) return 0;

            var d = ScriptableObject.CreateInstance<UnitData>();
            d.displayName = name; d.faction = faction; d.role = role; d.personality = personality; d.grade = grade;
            d.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + spritePath + ".png");
            if (d.sprite == null) Debug.LogWarning($"[UnitDataSeeder] 그림 없음: {spritePath}");
            d.isBoss = boss; d.shadowSize = boss ? ShadowSize.Large : ShadowSize.Normal;
            d.maxHp = hp; d.attack = atk; d.defense = def; d.attackInterval = interval; d.range = range;
            d.moveSpeed = move; d.critChance = crit; d.critMultiplier = critMul;
            d.attackType = type; d.areaRadius = area; d.shieldAmount = shield;
            d.poisonDamagePerSecond = poisonDps; d.poisonDuration = poisonDur; d.healAmount = heal; d.counterChance = counter;

            AssetDatabase.CreateAsset(d, path);
            return 1;
        }
    }
}
