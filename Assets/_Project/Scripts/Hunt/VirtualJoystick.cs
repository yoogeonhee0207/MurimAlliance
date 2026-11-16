using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 플로팅 조이스틱. 이 컴포넌트가 붙은 영역(화면 왼쪽 절반)을 누르면 그 자리에 조이스틱이 생기고,
    /// 끌면 방향과 세기(Value, 길이 0~1)를 낸다. 손을 떼면 사라지고 Value는 0이 된다.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static VirtualJoystick Current { get; private set; }

        [Tooltip("조이스틱 바탕(원)")] public RectTransform baseRect;
        [Tooltip("손잡이")] public RectTransform knobRect;
        [Tooltip("바탕 안쪽 어두운 원")] public RectTransform fillRect;
        [Tooltip("손잡이가 움직일 수 있는 반지름 (캔버스 단위)")] public float radius = 110f;
        [Tooltip("이 비율 안쪽은 입력 없음으로 침")] [Range(0f, 0.5f)] public float deadZone = 0.12f;

        /// <summary>현재 입력. 길이 0~1.</summary>
        public Vector2 Value { get; private set; }
        public bool IsPressed { get; private set; }

        RectTransform area;
        Canvas canvas;

        void Awake()
        {
            Current = this;
            area = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            SetCircle(baseRect, 6, new Color(1, 1, 1, 0.45f));
            SetCircle(fillRect, 0, new Color(0, 0, 0, 0.25f));
            SetCircle(knobRect, 0, new Color(1, 1, 1, 0.7f));
            Show(false);
        }

        void OnDestroy() { if (Current == this) Current = null; }

        public void OnPointerDown(PointerEventData e)
        {
            IsPressed = true;
            baseRect.anchoredPosition = ToLocal(e);
            knobRect.anchoredPosition = Vector2.zero;
            Show(true);
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            Vector2 offset = ToLocal(e) - baseRect.anchoredPosition;
            offset = Vector2.ClampMagnitude(offset, radius);
            knobRect.anchoredPosition = offset;

            Vector2 v = offset / radius;
            Value = v.magnitude < deadZone ? Vector2.zero : v;
        }

        public void OnPointerUp(PointerEventData e)
        {
            IsPressed = false;
            Value = Vector2.zero;
            Show(false);
        }

        Vector2 ToLocal(PointerEventData e)
        {
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, cam, out var local);
            // 영역의 기준점(pivot)이 왼쪽 아래(0,0)라서 local이 곧 바탕의 위치
            return local;
        }

        void Show(bool on)
        {
            if (baseRect != null) baseRect.gameObject.SetActive(on);
        }

        static void SetCircle(RectTransform rt, float ring, Color color)
        {
            if (rt == null) return;
            var img = rt.GetComponent<Image>();
            if (img == null || img.sprite != null) return;
            img.sprite = MakeCircleSprite(Mathf.RoundToInt(rt.sizeDelta.x), ring, color);
        }

        /// <summary>UI용 원 그림을 코드로 만든다 (ring=0이면 꽉 찬 원).</summary>
        public static Sprite MakeCircleSprite(int size, float ring, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size / 2f, inner = ring > 0 ? r - ring : -1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);                       // 바깥 가장자리 부드럽게
                if (inner > 0) a *= Mathf.Clamp01(d - inner);         // 안쪽 구멍
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
