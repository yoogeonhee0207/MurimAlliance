using UnityEngine;
using UnityEngine.InputSystem;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 맹주 직접 조작. 조이스틱(또는 에디터에서 WASD·방향키)으로 8방향 이동한다.
    /// 오브젝트 기준점 = 발밑. 나무·바위·맵 테두리와 부딪힌다(Rigidbody2D).
    /// 자동사냥·전투는 이후 단계에서 붙인다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class LeaderController : MonoBehaviour
    {
        [Tooltip("이동 속도 (칸/초)")] public float moveSpeed = 3.5f;
        [Tooltip("몸 그림 (좌우 반전·흔들림·그리는 순서 조정용)")] public SpriteRenderer body;

        /// <summary>지금 플레이어가 직접 조작 중인가 (자동사냥보다 우선)</summary>
        public bool IsManualInput => input.sqrMagnitude > 0.0001f;

#if UNITY_EDITOR
        // 에디터 테스트 전용: 정해진 시간 동안 이 입력을 넣는다 (Tools > 천하무림맹 > 테스트 메뉴)
        public static Vector2 DebugInput;
        public static float DebugUntil;

        // 이 프로젝트는 플레이 시작 때 도메인을 다시 불러오지 않아 static 값이 남는다 → 시작할 때 비운다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            DebugInput = Vector2.zero;
            DebugUntil = 0f;
        }
#endif

        Rigidbody2D rb;
        Vector2 input;
        Vector3 bodyBase;
        float bobTime;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (body != null) bodyBase = body.transform.localPosition;
        }

        void Update()
        {
            input = ReadInput();

            // 좌우 반전 (그림은 오른쪽을 봄)
            if (body != null && Mathf.Abs(input.x) > 0.05f) body.flipX = input.x < 0f;
        }

        void FixedUpdate()
        {
            rb.linearVelocity = input * moveSpeed;
        }

        void LateUpdate()
        {
            if (body == null) return;

            // 걷는 동안 살짝 통통 튀는 느낌 (1px 단위)
            if (IsManualInput)
            {
                bobTime += Time.deltaTime * 10f;
                float bob = Mathf.Abs(Mathf.Sin(bobTime)) >= 0.5f ? 1f / 32f : 0f;
                body.transform.localPosition = bodyBase + new Vector3(0, bob, 0);
            }
            else
            {
                bobTime = 0f;
                body.transform.localPosition = bodyBase;
            }

            // 발 위치가 아래일수록 앞에 그림 (나무와 같은 규칙)
            body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }

        static Vector2 ReadInput()
        {
#if UNITY_EDITOR
            if (Time.time < DebugUntil) return DebugInput;
#endif
            Vector2 v = VirtualJoystick.Current != null ? VirtualJoystick.Current.Value : Vector2.zero;

            var kb = Keyboard.current;
            if (v == Vector2.zero && kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;
                v = Vector2.ClampMagnitude(v, 1f);
            }
            return v;
        }
    }
}
