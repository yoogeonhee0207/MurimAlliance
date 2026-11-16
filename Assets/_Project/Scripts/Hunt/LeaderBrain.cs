using MurimAlliance.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 맹주의 판단.
    /// - 조이스틱(에디터에선 WASD·방향키)을 움직이는 동안: 그 방향으로 이동 (공격하지 않음)
    /// - 손을 떼고 있을 때: 사거리 안에 적이 있으면 공격
    /// 자동사냥(가까운 적을 찾아가기)과 쓰러진 뒤 부활은 4단계에서 붙인다.
    /// </summary>
    public class LeaderBrain : UnitBrain
    {
        [Tooltip("직접 조작할 때 이동 속도 (칸/초)")] public float moveSpeed = 3.5f;
        [Tooltip("손을 뗐을 때 이 거리 안의 적을 공격 대상으로 잡음 (사거리 + 여유)")] public float engageExtra = 0.3f;

        /// <summary>지금 플레이어가 직접 조작 중인가 (자동사냥보다 우선)</summary>
        public bool IsManualInput { get; private set; }

        Unit target;

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

        public override void Tick(Unit unit, float dt)
        {
            Vector2 input = ReadInput();
            IsManualInput = input.sqrMagnitude > 0.0001f;

            if (IsManualInput)
            {
                target = null;
                unit.Move(input, moveSpeed);
                return;
            }

            // 손을 뗐을 때: 지금 대상이 사거리 안이면 계속, 아니면 사거리 근처의 가장 가까운 적
            if (target == null || !target.IsAlive || !unit.InAttackRange(target))
                target = unit.FindNearestEnemy(unit.Data.range + engageExtra);

            if (target != null && unit.InAttackRange(target)) unit.AttackTick(target, dt);
            else unit.Stop();
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
