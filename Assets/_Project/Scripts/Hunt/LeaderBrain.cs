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
    /// - 자동사냥이 꺼져 있으면: 맞아도 반격하지 않음. 공격 버튼(에디터에선 스페이스바)을 한 번 누를 때마다
    ///   대상에게 다가가 한 대 때림 (공격 간격보다 빨리 때리지는 않음, 누르고 있으면 간격마다 계속)
    /// - 쓰러지면: 몇 초 뒤 입구에서 체력 10%로 부활하고 자동사냥은 꺼짐
    /// 길찾기가 아직 없어서, 장애물에 막혀 거의 못 움직이면 그 적은 잠시 포기하고 다른 적을 찾는다.
    /// </summary>
    public class LeaderBrain : UnitBrain
    {
        [Tooltip("직접 조작할 때 이동 속도 (칸/초)")] public float moveSpeed = 3.5f;
        [Tooltip("공격 버튼을 눌렀을 때 대상을 찾는 거리 (칸)")] public float manualTargetRange = 4f;
        [Tooltip("부활할 때 채워지는 체력 비율 (일부러 죽어서 체력을 채우는 것 방지)")] public float reviveHpRatio = 0.1f;
        [Tooltip("자동사냥 켜짐 여부")] public bool autoHunt;
        [Tooltip("쓰러진 뒤 부활까지 걸리는 시간 (초)")] public float respawnDelay = 5f;
        [Tooltip("이 시간 동안 거의 못 움직이면 막힌 것으로 보고 대상을 바꿈 (초)")] public float stuckTime = 0.8f;
        [Tooltip("막혀서 포기한 적을 다시 노리지 않는 시간 (초)")] public float ignoreTime = 3f;
        [Tooltip("이 시간 동안 맞지 않으면 체력이 차기 시작 (초)")] public float regenDelay = 3f;
        [Tooltip("전투 밖에서 초당 회복하는 비율 (최대 체력 대비)")] public float regenPerSecond = 0.04f;

        /// <summary>자동사냥이 켜지거나 꺼질 때 (버튼 표시 갱신용)</summary>
        public event System.Action<bool> AutoHuntChanged;

        /// <summary>지금 플레이어가 직접 조작 중인가 (자동사냥보다 우선)</summary>
        public bool IsManualInput { get; private set; }

        Unit target, self;
        int attackRequests;        // 눌렀지만 아직 못 때린 공격 (최대 1번 기억)

        /// <summary>맹주가 실제로 때린 횟수 (테스트 확인용)</summary>
        public int HitCount { get; private set; }

        /// <summary>공격 버튼을 누르고 있는 중인가 (누르고 있으면 간격마다 계속 공격)</summary>
        public bool AttackHeld { get; set; }

        /// <summary>공격 버튼을 한 번 누름: 대상을 정하고 한 대 때리기를 예약한다.</summary>
        public void RequestAttack()
        {
            attackRequests = 1;
        }
        Vector2 stuckFrom;
        float stuckTimer, regenAcc;
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
            u.Revive(pos, reviveHpRatio);
            target = null;
            attackRequests = 0;
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
            Regenerate(unit, dt);

            Vector2 input = ReadInput();
            IsManualInput = input.sqrMagnitude > 0.0001f;

            ReadAttackKey();

            if (IsManualInput)
            {
                target = null;
                attackRequests = 0;
                stuckTimer = 0f;
                unit.Move(input, moveSpeed);
                return;
            }

            if (autoHunt) AutoHunt(unit, dt);
            else ManualAttack(unit);
        }

        /// <summary>
        /// 자동사냥 꺼짐: 공격 버튼을 눌렀을 때만 공격. 맞아도 반격하지 않는다.
        /// 대상: 지금 나를 때리는 적이 먼저, 없으면 manualTargetRange 안의 가장 가까운 적.
        /// </summary>
        void ManualAttack(Unit unit)
        {
            bool wants = attackRequests > 0 || AttackHeld;
            if (!wants) { target = null; unit.Stop(); return; }

            if (target == null || !target.IsAlive ||
                (target.Position - unit.Position).sqrMagnitude > manualTargetRange * manualTargetRange * 1.5f)
            {
                var attacker = unit.LastAttacker;
                bool attackerNear = attacker != null && attacker.IsAlive &&
                    (attacker.Position - unit.Position).sqrMagnitude <= manualTargetRange * manualTargetRange;
                target = attackerNear ? attacker : unit.FindNearestEnemy(manualTargetRange);
            }
            if (target == null) { attackRequests = 0; unit.Stop(); return; }

            if (unit.InAttackRange(target))
            {
                if (unit.TryAttackOnce(target)) { attackRequests = 0; HitCount++; }
            }
            else unit.MoveTo(target.Position, moveSpeed);
        }

        /// <summary>에디터·PC용: 스페이스바 = 공격 버튼</summary>
        void ReadAttackKey()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) RequestAttack();
            if (kb.spaceKey.wasPressedThisFrame || kb.spaceKey.wasReleasedThisFrame) AttackHeld = kb.spaceKey.isPressed;
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
                if (unit.TryAttackOnce(target)) HitCount++;
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

        /// <summary>전투 밖(regenDelay초 동안 안 맞음)이면 체력이 천천히 찬다.</summary>
        void Regenerate(Unit unit, float dt)
        {
            if (unit.CurrentHp >= unit.Data.maxHp || Time.time - unit.LastDamagedTime < regenDelay)
            {
                regenAcc = 0f;
                return;
            }
            regenAcc += unit.Data.maxHp * regenPerSecond * dt;
            int whole = Mathf.FloorToInt(regenAcc);
            if (whole > 0) { unit.Heal(whole); regenAcc -= whole; }
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
