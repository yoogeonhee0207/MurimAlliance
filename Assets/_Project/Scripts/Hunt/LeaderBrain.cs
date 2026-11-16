using System.Collections;
using System.Collections.Generic;
using MurimAlliance.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 맹주의 판단.
    /// - 조이스틱(에디터에선 WASD·방향키)을 움직이는 동안: 그 방향으로 이동 (공격하지 않음, 자동사냥보다 우선)
    /// - 손을 떼고 있고 자동사냥이 켜져 있으면: 지금 있는 구역에서 가장 가까운 적을 찾아가 공격
    /// - 손을 떼고 있고 자동사냥이 꺼져 있으면: 사거리 안에 적이 있을 때만 공격
    /// - 쓰러지면: 몇 초 뒤 입구에서 부활하고 자동사냥은 꺼짐
    /// 길찾기가 아직 없어서, 장애물에 막혀 거의 못 움직이면 그 적은 잠시 포기하고 다른 적을 찾는다.
    /// </summary>
    public class LeaderBrain : UnitBrain
    {
        [Tooltip("직접 조작할 때 이동 속도 (칸/초)")] public float moveSpeed = 3.5f;
        [Tooltip("손을 뗐을 때 이 거리 안의 적을 공격 대상으로 잡음 (사거리 + 여유)")] public float engageExtra = 0.3f;
        [Tooltip("자동사냥 켜짐 여부")] public bool autoHunt;
        [Tooltip("쓰러진 뒤 부활까지 걸리는 시간 (초)")] public float respawnDelay = 5f;
        [Tooltip("이 시간 동안 거의 못 움직이면 막힌 것으로 보고 대상을 바꿈 (초)")] public float stuckTime = 0.8f;
        [Tooltip("막혀서 포기한 적을 다시 노리지 않는 시간 (초)")] public float ignoreTime = 3f;

        /// <summary>자동사냥이 켜지거나 꺼질 때 (버튼 표시 갱신용)</summary>
        public event System.Action<bool> AutoHuntChanged;

        /// <summary>지금 플레이어가 직접 조작 중인가 (자동사냥보다 우선)</summary>
        public bool IsManualInput { get; private set; }

        Unit target, self;
        Vector2 stuckFrom;
        float stuckTimer;
        readonly Dictionary<Unit, float> ignoredUntil = new Dictionary<Unit, float>();

        public void SetAutoHunt(bool on)
        {
            if (autoHunt == on) return;
            autoHunt = on;
            target = null;
            Debug.Log($"[Hunt] 자동사냥 {(on ? "켜짐" : "꺼짐")}");
            AutoHuntChanged?.Invoke(on);
        }

        public void ToggleAutoHunt() => SetAutoHunt(!autoHunt);

        void Start()
        {
            self = GetComponent<Unit>();
            if (self != null) self.Died += OnDied;
        }

        void OnDestroy()
        {
            if (self != null) self.Died -= OnDied;
        }

        void OnDied(Unit u)
        {
            SetAutoHunt(false);
            StartCoroutine(RespawnAfterDelay(u));
        }

        IEnumerator RespawnAfterDelay(Unit u)
        {
            yield return new WaitForSeconds(respawnDelay);
            var map = HuntMap.Current;
            Vector2 pos = map != null && map.spawnPoint != null ? (Vector2)map.spawnPoint.position : u.Position;
            u.Revive(pos);
            target = null;
        }

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
                stuckTimer = 0f;
                unit.Move(input, moveSpeed);
                return;
            }

            if (autoHunt) AutoHunt(unit, dt);
            else AttackInRange(unit, dt);
        }

        /// <summary>자동사냥 꺼짐: 사거리 근처의 적만 공격, 쫓아가지 않음.</summary>
        void AttackInRange(Unit unit, float dt)
        {
            if (target == null || !target.IsAlive || !unit.InAttackRange(target))
                target = unit.FindNearestEnemy(unit.Data.range + engageExtra);

            if (target != null && unit.InAttackRange(target)) unit.AttackTick(target, dt);
            else unit.Stop();
        }

        /// <summary>자동사냥 켜짐: 지금 구역 안의 가장 가까운 적을 찾아가 공격.</summary>
        void AutoHunt(Unit unit, float dt)
        {
            var map = HuntMap.Current;
            var zone = map != null ? map.ZoneAt(unit.Position) : null;

            if (target == null || !target.IsAlive || IsIgnored(target))
            {
                target = unit.FindNearestEnemy(float.MaxValue,
                    u => !IsIgnored(u) && (zone == null || zone.Contains(u.Position)));
                stuckTimer = 0f;
                stuckFrom = unit.Position;
            }
            if (target == null) { unit.Stop(); return; }

            if (unit.InAttackRange(target))
            {
                stuckTimer = 0f;
                unit.AttackTick(target, dt);
                return;
            }

            unit.MoveTo(target.Position, moveSpeed);

            // 막힘 검사: stuckTime 동안 0.3칸도 못 갔으면 이 적은 잠시 포기
            stuckTimer += dt;
            if (stuckTimer >= stuckTime)
            {
                if ((unit.Position - stuckFrom).sqrMagnitude < 0.09f)
                {
                    ignoredUntil[target] = Time.time + ignoreTime;
                    Debug.Log($"[Hunt] 길이 막혀 {target.Data.displayName}을(를) 잠시 포기");
                    target = null;
                }
                stuckTimer = 0f;
                stuckFrom = unit.Position;
            }
        }

        bool IsIgnored(Unit u) => ignoredUntil.TryGetValue(u, out float until) && Time.time < until;

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
