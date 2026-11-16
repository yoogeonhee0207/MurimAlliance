using MurimAlliance.Data;
using UnityEngine;

namespace MurimAlliance.Battle
{
    /// <summary>유닛 데이터로 전투 유닛(몸, 그림자, 체력 막대)을 만든다.</summary>
    public static class UnitFactory
    {
        public static Unit Spawn(UnitData data, Team team, Vector2 pos, Sprite shadowNormal, Sprite shadowLarge)
        {
            var root = new GameObject($"{data.displayName} ({team})");
            root.transform.position = pos;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root.transform, false);
            var body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = data.sprite;

            float half = data.sprite != null ? data.sprite.bounds.extents.y : 0.5f;

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, -half + 0.125f, 0f);   // 발밑
            var shadow = shadowGo.AddComponent<SpriteRenderer>();
            shadow.sprite = data.shadowSize == ShadowSize.Large ? shadowLarge : shadowNormal;

            var bar = HpBar.Create(root.transform, half + 0.1f,
                team == Team.Ally ? new Color(0.35f, 0.85f, 0.4f) : new Color(0.9f, 0.3f, 0.3f));

            var unit = root.AddComponent<Unit>();
            unit.Init(data, team, body, shadow, bar);
            return unit;
        }
    }
}
