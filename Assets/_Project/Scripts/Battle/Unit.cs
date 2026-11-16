using System.Collections.Generic;
using MurimAlliance.Data;
using UnityEngine;

namespace MurimAlliance.Battle
{
    public enum Team { Ally, Enemy }
    public enum UnitState { Idle, Move, Attack, Dead }

    /// <summary>
    /// 전투 유닛 한 명. 흐름: 대기 → 가장 가까운 적 찾기 → 사거리까지 이동 → 공격 → 피격 → 쓰러짐.
    /// 현재는 공격 방식 중 "근접 단일"만 구현. 거리 단위는 칸(1칸 = 1 유닛 = 32px).
    /// </summary>
    public class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

        public UnitData Data { get; private set; }
        public Team Team { get; private set; }
        public UnitState State { get; private set; } = UnitState.Idle;
        public int CurrentHp { get; private set; }
        public bool IsAlive => State != UnitState.Dead;

        SpriteRenderer body, shadow;
        HpBar hpBar;
        Unit target;
        float attackTimer, hurtTimer, lungeTimer, bobPhase, deathTimer;
        Vector3 bodyBase;

        const float HurtFlash = 0.12f, LungeTime = 0.15f, LungeDist = 0.25f, BobHeight = 0.03f, DeathFade = 0.6f;
        static readonly Color HurtColor = new Color(1f, 0.45f, 0.45f);

        public void Init(UnitData data, Team team, SpriteRenderer bodyRenderer, SpriteRenderer shadowRenderer, HpBar bar)
        {
            Data = data; Team = team;
            body = bodyRenderer; shadow = shadowRenderer; hpBar = bar;
            bodyBase = body.transform.localPosition;
            CurrentHp = data.maxHp;
            attackTimer = data.attackInterval * 0.5f;   // 첫 공격은 간격의 절반 뒤
            bobPhase = Random.value * Mathf.PI * 2f;
            hpBar.Set(1f);
            body.flipX = team == Team.Enemy;           // 그림은 오른쪽을 봄. 적은 처음에 왼쪽을 보게
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (Data == null) return;
            float dt = Time.deltaTime;

            if (!IsAlive) { UpdateDeath(dt); return; }
            UpdateVisuals(dt);

            if (target == null || !target.IsAlive) target = FindNearestEnemy();
            if (target == null) { State = UnitState.Idle; return; }

            Vector3 to = target.transform.position - transform.position;
            body.flipX = to.x < 0f;

            if (to.magnitude > Data.range)
            {
                State = UnitState.Move;
                transform.position += to.normalized * (Data.moveSpeed * dt);
            }
            else
            {
                State = UnitState.Attack;
                attackTimer -= dt;
                if (attackTimer <= 0f)
                {
                    attackTimer = Data.attackInterval;
                    Attack(target);
                }
            }
        }

        Unit FindNearestEnemy()
        {
            Unit best = null; float bestDist = float.MaxValue;
            foreach (var u in All)
            {
                if (u == this || !u.IsAlive || u.Team == Team) continue;
                float d = (u.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = u; }
            }
            return best;
        }

        void Attack(Unit t)
        {
            lungeTimer = LungeTime;
            bool crit = Random.value < Data.critChance;
            int dmg = CalcDamage(Data.attack, t.Data.defense);
            if (crit) dmg = Mathf.RoundToInt(dmg * Data.critMultiplier);
            t.TakeDamage(dmg, crit);
        }

        /// <summary>피해 = 공격력 × 100 ÷ (100 + 방어력), 최소 1. 방어력 100이면 피해가 절반.</summary>
        public static int CalcDamage(int attack, int defense) =>
            Mathf.Max(1, Mathf.RoundToInt(attack * 100f / (100f + defense)));

        public void TakeDamage(int dmg, bool crit)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Max(0, CurrentHp - dmg);
            hurtTimer = HurtFlash;
            hpBar.Set((float)CurrentHp / Data.maxHp);
            if (CurrentHp == 0) Die();
        }

        void Die()
        {
            State = UnitState.Dead;
            deathTimer = DeathFade;
            hpBar.gameObject.SetActive(false);
            body.color = Color.white;
            Debug.Log($"[Battle] {Data.displayName}({Team}) 쓰러짐");
        }

        void UpdateVisuals(float dt)
        {
            // 대기 중 위아래로 살짝 흔들림 + 공격할 때 앞으로 튀어나감
            bobPhase += dt * 4f;
            float bob = State == UnitState.Move ? 0f : Mathf.Sin(bobPhase) * BobHeight;
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

            // 아래쪽에 있는 유닛이 앞에 그려지게
            int order = Mathf.RoundToInt(-transform.position.y * 100f);
            body.sortingOrder = order;
            shadow.sortingOrder = order - 1;
        }

        void UpdateDeath(float dt)
        {
            deathTimer -= dt;
            float a = Mathf.Clamp01(deathTimer / DeathFade);
            body.color = new Color(1f, 1f, 1f, a);
            shadow.color = new Color(1f, 1f, 1f, a);
            if (deathTimer <= 0f) Destroy(gameObject);
        }
    }
}
