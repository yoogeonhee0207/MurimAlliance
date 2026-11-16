using UnityEngine;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 카메라가 대상(맹주)을 따라간다. 맵 경계 밖은 비추지 않게 막고,
    /// 도트가 흔들리지 않게 위치를 1픽셀(1/32칸) 단위로 맞춘다.
    /// 부드럽게 늦게 따라가는 방식은 픽셀 단위 맞춤과 섞이면 떨려 보여서 쓰지 않는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [Tooltip("대상 발밑에서 화면 중심까지 올릴 높이 (칸)")] public float yOffset = 0.5f;
        [Tooltip("픽셀 단위 (32 PPU)")] public int pixelsPerUnit = 32;

        Camera cam;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 p = target.position + new Vector3(0, yOffset, 0);

            var map = HuntMap.Current;
            if (map != null)
            {
                // Pixel Perfect Camera가 실행 중에 orthographicSize를 정하므로 매 프레임 읽는다
                float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
                var b = map.bounds;
                p.x = b.width <= halfW * 2 ? b.center.x : Mathf.Clamp(p.x, b.xMin + halfW, b.xMax - halfW);
                p.y = b.height <= halfH * 2 ? b.center.y : Mathf.Clamp(p.y, b.yMin + halfH, b.yMax - halfH);
            }

            float ppu = pixelsPerUnit;
            p.x = Mathf.Round(p.x * ppu) / ppu;
            p.y = Mathf.Round(p.y * ppu) / ppu;
            p.z = transform.position.z;
            transform.position = p;
        }
    }
}
