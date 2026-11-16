using UnityEngine;

namespace MurimAlliance.Battle
{
    /// <summary>머리 위 체력 막대. 아군은 초록, 적은 빨강.</summary>
    public class HpBar : MonoBehaviour
    {
        const float Width = 0.75f, Height = 2f / 32f;   // 2px 두께
        Transform fill;
        static Sprite white;

        public static HpBar Create(Transform parent, float y, Color color)
        {
            var go = new GameObject("HpBar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            var bar = go.AddComponent<HpBar>();
            MakeRect(go.transform, "Back", new Color(0.1f, 0.1f, 0.12f, 0.9f), 1000);
            bar.fill = MakeRect(go.transform, "Fill", color, 1001);
            return bar;
        }

        public void Set(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            fill.localScale = new Vector3(Width * ratio, Height, 1f);
            fill.localPosition = new Vector3(-Width * 0.5f + Width * ratio * 0.5f, 0f, 0f);
        }

        static Transform MakeRect(Transform parent, string name, Color color, int order)
        {
            if (white == null)
            {
                var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white); tex.Apply();
                white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(Width, Height, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = white; sr.color = color; sr.sortingOrder = order;
            return go.transform;
        }
    }
}
