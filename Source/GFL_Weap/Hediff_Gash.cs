using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Gash hediff - DOT with stack consumption.
    /// Deals 8% of applier attack per stack every 60 ticks, then consumes 2 stacks.
    /// </summary>
    public class Hediff_Gash : HediffWithComps
    {
        // Stack count
        private int stacks = 0;

        // Tick counter for DOT intervals
        private int tickCounter = 0;

        // Reference to applier pawn
        private Pawn applierPawn = null;

        // Applier weapon damage (cached)
        private float applierAttackDamage = 10f;

        // Damage interval (60 ticks = 1 second)
        private const int DAMAGE_INTERVAL = 60;

        // Damage multiplier per stack
        private const float DAMAGE_MULTIPLIER_PER_STACK = 0.08f;

        // Stacks consumed per tick
        private const int STACKS_CONSUMED_PER_TICK = 2;

        public int Stacks => stacks;
        public Pawn Applier => applierPawn;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            try
            {
                // Initialize from severity
                if (this.Severity > 0)
                {
                    stacks = Mathf.Max(1, Mathf.RoundToInt(this.Severity));
                }
                else
                {
                    stacks = 1;
                }

                this.Severity = stacks;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_Gash.PostAdd error: {ex.Message}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                if (pawn == null || pawn.Dead)
                {
                    return;
                }

                tickCounter++;

                // Apply DOT every DAMAGE_INTERVAL ticks
                if (tickCounter >= DAMAGE_INTERVAL)
                {
                    tickCounter = 0;
                    ApplyDamageOverTime();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_Gash.Tick error: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply DOT damage and consume stacks.
        /// </summary>
        private void ApplyDamageOverTime()
        {
            try
            {
                if (pawn == null || pawn.Dead || pawn.Map == null)
                {
                    return;
                }

                if (stacks <= 0)
                {
                    // Remove hediff if no stacks remain
                    pawn.health.RemoveHediff(this);
                    return;
                }

                // Calculate damage: applierAttack * 0.08 * stacks
                float totalDamage = applierAttackDamage * DAMAGE_MULTIPLIER_PER_STACK * stacks;

                // Apply damage
                DamageInfo dinfo = new DamageInfo(
                    DamageDefOf.Cut,
                    Mathf.RoundToInt(totalDamage),
                    0f,
                    -1f,
                    applierPawn,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown
                );

                pawn.TakeDamage(dinfo);

                // Visual feedback
                if (pawn.Map != null)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, $"-{Mathf.RoundToInt(totalDamage)} (gash)", 1.9f);
                }

                // Consume stacks
                stacks = Mathf.Max(0, stacks - STACKS_CONSUMED_PER_TICK);
                this.Severity = stacks;

                // Remove if depleted
                if (stacks <= 0)
                {
                    pawn.health.RemoveHediff(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_Gash.ApplyDamageOverTime error: {ex.Message}");
            }
        }

        /// <summary>
        /// Set applier and cache their attack damage.
        /// </summary>
        public void SetApplier(Pawn applier, float attackDamage)
        {
            this.applierPawn = applier;
            this.applierAttackDamage = attackDamage;
        }

        /// <summary>
        /// Add stacks.
        /// </summary>
        public void AddStacks(int amount)
        {
            if (amount <= 0) return;

            stacks += amount;
            this.Severity = stacks;
        }

        /// <summary>
        /// Save/load data.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stacks, "stacks", 0);
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
            Scribe_Values.Look(ref applierAttackDamage, "applierAttackDamage", 10f);
            Scribe_References.Look(ref applierPawn, "applierPawn");
        }

        /// <summary>
        /// Merge with existing Gash hediff.
        /// </summary>
        public override bool TryMergeWith(Hediff other)
        {
            if (other is Hediff_Gash otherGash)
            {
                // Add stacks
                AddStacks(otherGash.stacks);
                return true;
            }

            return false;
        }

        public override string LabelInBrackets
        {
            get
            {
                return $"x{stacks}";
            }
        }
    }
}
