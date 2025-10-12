using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// ScorchMark hediff - Applies fire DOT damage every 60 ticks.
    /// Stacks multiplicatively: baseDamage * (1 + 0.07 * (stacks - 1))
    /// Cannot be cleansed, must expire naturally after 1800 ticks (30 seconds).
    /// </summary>
    public class Hediff_ScorchMark : HediffWithComps
    {
        // Tick counter for DOT intervals
        private int tickCounter = 0;

        // Number of ScorchMark stacks
        private int stacks = 1;

        // Reference to the pawn who applied this hediff (for damage calculation)
        private Pawn applierPawn = null;

        // Duration in ticks (1800 = 30 seconds = 3 turns)
        private const int DURATION_TICKS = 1800;

        // Interval between damage ticks (60 ticks = 1 second)
        private const int DAMAGE_INTERVAL = 60;

        // Base damage multiplier per tick (7% of caster's attack)
        private const float BASE_DAMAGE_MULTIPLIER = 0.07f;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            try
            {
                // Initialize stacks from severity (severity represents stack count)
                if (this.Severity > 0)
                {
                    stacks = Mathf.Max(1, Mathf.RoundToInt(this.Severity));
                }

                // Set initial severity to track duration
                if (this.Severity < 1f)
                {
                    this.Severity = 1f;
                }

                // Log for debugging
                // Log.Message($"[GFL Weapons] ScorchMark applied to {pawn?.LabelShort ?? "unknown"}, stacks: {stacks}");
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_ScorchMark.PostAdd error: {ex.Message}");
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

                // Auto-remove after duration expires
                if (this.ageTicks >= DURATION_TICKS)
                {
                    pawn.health.RemoveHediff(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_ScorchMark.Tick error: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply fire damage based on applier's attack power and stack count.
        /// </summary>
        private void ApplyDamageOverTime()
        {
            try
            {
                if (pawn == null || pawn.Dead || pawn.Map == null)
                {
                    return;
                }

                // Calculate base damage from applier's weapon
                float baseDamage = 5f; // Default fallback

                if (applierPawn != null && applierPawn.equipment != null && applierPawn.equipment.Primary != null)
                {
                    baseDamage = WeaponDamageResolver.GetWeaponBaseDamage(applierPawn, applierPawn.equipment.Primary);
                }

                // Calculate tick damage: baseDamage * 0.07 per stack
                float baseTickDamage = baseDamage * BASE_DAMAGE_MULTIPLIER;

                // Apply multiplicative stacking: baseTick * (1 + 0.07 * (stacks - 1))
                float totalDamage = baseTickDamage * (1f + 0.07f * (stacks - 1));

                // Apply damage as Flame type
                DamageInfo dinfo = new DamageInfo(
                    DamageDefOf.Flame,
                    Mathf.RoundToInt(totalDamage),
                    0f,
                    -1f,
                    applierPawn,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown
                );

                pawn.TakeDamage(dinfo);

                // Visual feedback - show damage number
                if (pawn.Map != null)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, $"-{Mathf.RoundToInt(totalDamage)} (burn)", 1.9f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_ScorchMark.ApplyDamageOverTime error: {ex.Message}");
            }
        }

        /// <summary>
        /// Set the pawn who applied this hediff (for damage calculations).
        /// </summary>
        public void SetApplier(Pawn applier)
        {
            this.applierPawn = applier;
        }

        /// <summary>
        /// Increase the stack count when another ScorchMark is applied.
        /// </summary>
        public void AddStack()
        {
            stacks++;
            this.Severity = stacks; // Update severity to reflect stack count
        }

        /// <summary>
        /// Save/load hediff data across game saves.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
            Scribe_Values.Look(ref stacks, "stacks", 1);
            Scribe_References.Look(ref applierPawn, "applierPawn");
        }

        /// <summary>
        /// Prevent normal curing - must expire naturally.
        /// </summary>
        public override bool TryMergeWith(Hediff other)
        {
            if (other is Hediff_ScorchMark otherScorch)
            {
                // Merge by adding stacks
                AddStack();

                // Reset duration to maximum
                this.ageTicks = 0;

                return true;
            }

            return false;
        }
    }
}
