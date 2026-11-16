using MurimAlliance.Data;
using UnityEngine;

namespace MurimAlliance.Battle
{
    /// <summary>
    /// 유닛 데이터로 전투 유닛을 만든다. 기준점 = 발밑.
    /// 루트: Rigidbody2D(중력 없음) + 발 충돌 원 / 자식: Body(그림), Shadow(그림자), HpBar(체력 막대).
    /// 판단(UnitBrain)은 부르는 쪽에서 붙인다. 예: UnitFactory.Spawn(...).gameObject.AddComponent&lt;ArenaBrain&gt;()
    /// </summary>
    public static class UnitFactory
    {
        const float FeetInset = 0.1f;   // 그림 맨 아래 투명한 줄만큼 몸을 내림

        public static Unit Spawn(UnitData data, Team team, Vector2 feetPos, Sprite shadowNormal, Sprite shadowLarge)
        {
            var root = new GameObject($"{data.displayName} ({team})");
            root.transform.position = feetPos;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = root.AddComponent<CircleCollider2D>();
            bool big = data.shadowSize == ShadowSize.Large;
            col.radius = big ? 0.35f : 0.22f;
            col.offset = new Vector2(0f, 0.1f);

            float half = data.sprite != null ? data.sprite.bounds.extents.y : 0.5f;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, half - FeetInset, 0f);
            var body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = data.sprite;

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var shadow = shadowGo.AddComponent<SpriteRenderer>();
            shadow.sprite = big ? shadowLarge : shadowNormal;

            var bar = HpBar.Create(root.transform, half * 2f - FeetInset + 0.1f,
                team == Team.Ally ? new Color(0.35f, 0.85f, 0.4f) : new Color(0.9f, 0.3f, 0.3f));

            var unit = root.AddComponent<Unit>();
            unit.Init(data, team, body, shadow, bar);
            return unit;
        }
    }
}
