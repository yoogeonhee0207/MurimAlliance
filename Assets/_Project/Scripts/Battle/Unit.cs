using System;
using System.Collections.Generic;
using MurimAlliance.Data;
using UnityEngine;

namespace MurimAlliance.Battle
{
    public enum Team { Ally, Enemy }
    public enum UnitState { Idle, Move, Attack, Dead }

    /// <summary>
    /// 전투 유닛 한 명: 능력치·체력·공격·피격·쓰러짐·연출을 맡는다.
    /// "누구를 쫓고 언제 공격할지"는 같은 오브젝트의 UnitBrain(판단)이 정해서 Move / AttackTick을 부른다.
    /// 기준점 = 발밑. 이동은 Rigidbody2D로 해서 장애물에 막히고 유닛끼리 살짝 밀어낸다.
    /// 현재는 공격 방식 중 "근접 단일"만 구현. 거리 단위는 칸(1칸 = 1 유닛 = 32px).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

        public UnitData Data { get; private set; }
        public Team Team { get; private set; }
        public UnitState State { get; private set; } = UnitState.Idle;
        public int CurrentHp { get; private set; }
        public bool IsAlive => State != UnitState.Dead;
        public Vector2 Position => transform.position;

        /// <summary>쓰러지면 사라질지. 맹주처럼 부활하는 유닛은 false.</summary>
        public bool destroyOnDeath = true;

        /// <summary>쓰러진 순간 한 번 불린다.</summary>
        public event Action<Unit> Died;

        SpriteRenderer body, shadow;
        HpBar hpBar;
        Rigidbody2D rb;
        UnitBrain brain;
        Vector2 velocity;
        float attackTimer, hurtTimer, lungeTimer, bobPhase, walkPhase, deathTimer;
        Vector3 bodyBase;

        const float HurtFlash = 0.12f, LungeTime = 0.15f, LungeDist = 0.25f, BobHeight = 0.03f, DeathFade = 0.6f;
        static readonly Color HurtColor = new Color(1f, 0.45f, 0.45f);

        public void Init(UnitData data, Team team, SpriteRenderer bodyRenderer, SpriteRenderer shadowRenderer, HpBar bar)
        {
            Data = data; Team = team;
            body = bodyRenderer; shadow = shadowRenderer; hpBar = bar;
            bodyBase = body.transform.localPosition;
            rb = GetComponent<Rigidbody2D>();
            CurrentHp = data.maxHp;
            attackTimer = data.attackInterval * 0.5f;   // 첫 공격은 간격의 절반 뒤
            bobPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            hpBar.Set(1f);
            body.flipX = team == Team.Enemy;           // 그림은 오른쪽을 봄. 적은 처음에 왼쪽을 보게
        }

        void Awake() => brain = GetComponent<UnitBrain>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (Data == null) return;
            float dt = Time.deltaTime;

            if (!IsAlive) { UpdateDeath(dt); return; }

            if (attackTimer > 0f) attackTimer -= dt;   // 공격 간격(대기 시간)은 항상 흐름

            if (brain == null) brain = GetComponent<UnitBrain>();
            if (brain != null) brain.Tick(this, dt);
            else Stop();

            UpdateVisuals(dt);
        }

        void FixedUpdate()
        {
            if (rb == null) return;
            rb.linearVelocity = IsAlive ? velocity : Vector2.zero;
            // 멈춰 있거나 공격 중인 유닛은 무겁게: 걸어오는 유닛이 밀고 가지 못하고 비켜 가게 한다 (살짝만 밀림)
            rb.mass = velocity.sqrMagnitude > 0.0001f ? MovingMass : StandingMass;
        }

        const float MovingMass = 1f, StandingMass = 30f;

        // ---------- 판단(UnitBrain)이 부르는 동작 ----------

        /// <summary>방향(길이 0~1)으로 이동. speed를 주지 않으면 데이터의 이동 속도.</summary>
        public void Move(Vector2 dir, float speed = -1f)
        {
            if (!IsAlive) return;
            if (speed < 0f) speed = Data.moveSpeed;
            velocity = Vector2.ClampMagnitude(dir, 1f) * speed;
            State = velocity.sqrMagnitude > 0.0001f ? UnitState.Move : UnitState.Idle;
            if (Mathf.Abs(dir.x) > 0.05f) body.flipX = dir.x < 0f;
        }

        /// <summary>목표 지점으로 이동.</summary>
        public void MoveTo(Vector2 point, float speed = -1f)
        {
            Vector2 to = point - Position;
            Move(to.sqrMagnitude > 0.0025f ? to.normalized : Vector2.zero, speed);
        }

        public void Stop()
        {
            velocity = Vector2.zero;
            if (IsAlive) State = UnitState.Idle;
        }

        /// <summary>상대가 사거리 안에 있는가 (발밑 사이 거리).</summary>
        public bool InAttackRange(Unit t) =>
            t != null && (t.Position - Position).sqrMagnitude <= Data.range * Data.range;

        /// <summary>공격 간격이 다 차서 바로 때릴 수 있는가.</summary>
        public bool AttackReady => attackTimer <= 0f;

        /// <summary>사거리 안의 상대를 향해 멈춰 서서 공격 간격마다 공격한다. 매 프레임 부른다.</summary>
        public void AttackTick(Unit t, float dt) => TryAttackOnce(t);

        /// <summary>
        /// 사거리 안의 상대를 향해 멈춰 서고, 공격 간격이 찼으면 한 대 때린다.
        /// 때렸으면 true (간격이 아직이면 false).
        /// </summary>
        public bool TryAttackOnce(Unit t)
        {
            if (!IsAlive || t == null || !t.IsAlive) return false;
            velocity = Vector2.zero;
            State = UnitState.Attack;
            float dx = t.Position.x - Position.x;
            if (Mathf.Abs(dx) > 0.05f) body.flipX = dx < 0f;

            if (attackTimer > 0f) return false;
            attackTimer = Data.attackInterval;
            Attack(t);
            return true;
        }

        /// <summary>조건에 맞는 살아 있는 적 중 가장 가까운 유닛. maxDist 밖은 무시.</summary>
        public Unit FindNearestEnemy(float maxDist = float.MaxValue, Predicate<Unit> filter = null)
        {
            Unit best = null;
            float bestDist = maxDist * maxDist;
            foreach (var u in All)
            {
                if (u == this || !u.IsAlive || u.Team == Team) continue;
                if (filter != null && !filter(u)) continue;
                float d = (u.Position - Position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = u; }
            }
            return best;
        }

        // ---------- 공격·피격·쓰러짐 ----------

        void Attack(Unit t)
        {
            lungeTimer = LungeTime;
            bool crit = UnityEngine.Random.value < Data.critChance;
            int dmg = CalcDamage(Data.attack, t.Data.defense);
            if (crit) dmg = Mathf.RoundToInt(dmg * Data.critMultiplier);
            t.TakeDamage(dmg, crit, this);
        }

        /// <summary>피해 = 공격력 × 100 ÷ (100 + 방어력), 최소 1. 방어력 100이면 피해가 절반.</summary>
        public static int CalcDamage(int attack, int defense) =>
            Mathf.Max(1, Mathf.RoundToInt(attack * 100f / (100f + defense)));

        /// <summary>마지막으로 나를 때린 상대 (몬스터가 반격할 때 씀).</summary>
        public Unit LastAttacker { get; private set; }

        /// <summary>마지막으로 피해를 받은 시각 (전투 밖 회복 판단용).</summary>
        public float LastDamagedTime { get; private set; } = -999f;

        public void ClearLastAttacker() => LastAttacker = null;

        /// <summary>체력 회복 (최대 체력을 넘지 않음).</summary>
        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0 || CurrentHp >= Data.maxHp) return;
            CurrentHp = Mathf.Min(Data.maxHp, CurrentHp + amount);
            hpBar.Set((float)CurrentHp / Data.maxHp);
        }

        public void RestoreFullHp() => Heal(Data.maxHp);

        public void TakeDamage(int dmg, bool crit, Unit attacker = null)
        {
            if (!IsAlive) return;
            if (attacker != null) LastAttacker = attacker;
            LastDamagedTime = Time.time;
            CurrentHp = Mathf.Max(0, CurrentHp - dmg);
            hurtTimer = HurtFlash;
            hpBar.Set((float)CurrentHp / Data.maxHp);
            if (CurrentHp == 0) Die();
        }

        void Die()
        {
            State = UnitState.Dead;
            velocity = Vector2.zero;
            deathTimer = DeathFade;
            hpBar.gameObject.SetActive(false);
            body.color = Color.white;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;      // 쓰러진 몸은 길을 막지 않음
            Debug.Log($"[Battle] {Data.displayName}({Team}) 쓰러짐");
            Died?.Invoke(this);
        }

        /// <summary>부활: 지정 위치에 다시 세우고 체력을 hpRatio만큼 채운다.</summary>
        public void Revive(Vector2 pos, float hpRatio = 1f)
        {
            transform.position = pos;
            if (rb != null) { rb.position = pos; rb.linearVelocity = Vector2.zero; }
            CurrentHp = Mathf.Max(1, Mathf.RoundToInt(Data.maxHp * Mathf.Clamp01(hpRatio)));
            State = UnitState.Idle;
            LastAttacker = null;
            attackTimer = Data.attackInterval * 0.5f;
            hpBar.gameObject.SetActive(true);
            hpBar.Set((float)CurrentHp / Data.maxHp);
            body.color = Color.white;
            shadow.color = Color.white;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
            Debug.Log($"[Battle] {Data.displayName}({Team}) 부활 (체력 {CurrentHp}/{Data.maxHp})");
        }

        // ---------- 연출 ----------

        void UpdateVisuals(float dt)
        {
            // 걷는 중: 1px 통통 / 서 있을 때: 위아래로 살짝 흔들림 / 공격: 앞으로 튀어나감
            float bob;
            if (State == UnitState.Move)
            {
                walkPhase += dt * 10f;
                bob = Mathf.Abs(Mathf.Sin(walkPhase)) >= 0.5f ? 1f / 32f : 0f;
            }
            else
            {
                walkPhase = 0f;
                bobPhase += dt * 4f;
                bob = Mathf.Sin(bobPhase) * BobHeight;
            }
            float lunge = 0f;
            if (lungeTimer > 0f)
            {
                lungeTimer -= dt;
                lunge = Mathf.Sin(Mathf.PI * (1f - lungeTimer / LungeTime)) * LungeDist * (body.flipX ? -1f : 1f);
            }
            body.transform.localPosition = bodyBase + new Vector3(lunge, bob, 0f);

            // 맞으면 잠깐 붉게
            if (hurtTimer > 0f) hurtTimer -= dt;
            body.color = Color.Lerp(Color.white, HurtColor, Mathf.Clamp01(hurtTimer / HurtFlash));

            // 발이 아래쪽인 유닛이 앞에 그려지게 (나무·바위와 같은 규칙). 그림자는 바닥 바로 위.
            body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
            shadow.sortingOrder = -31000;
        }

        void UpdateDeath(float dt)
        {
            if (!destroyOnDeath) { body.color = new Color(1f, 1f, 1f, 0.35f); return; }
            deathTimer -= dt;
            float a = Mathf.Clamp01(deathTimer / DeathFade);
            body.color = new Color(1f, 1f, 1f, a);
            shadow.color = new Color(1f, 1f, 1f, a);
            if (deathTimer <= 0f) Destroy(gameObject);
        }
    }
}
