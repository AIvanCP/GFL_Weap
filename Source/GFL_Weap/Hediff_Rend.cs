using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Rend hediff - Stackable defense debuff (max 8 stacks).
    /// Each stack increases physical damage taken by 30%.
    /// Damage multiplier formula: 1 + (0.30 * stacks)
    /// </summary>
    public class Hediff_Rend : HediffWithComps
    {
        // Stack count (max 8)
        private int stacks = 0;

        // Reference to applier pawn
        private Pawn applierPawn = null;

        // Duration in ticks (15 seconds = 900 ticks)
        private const int DURATION_TICKS = 900;

        // Max stacks
        private const int MAX_STACKS = 8;

        // Damage multiplier per stack
        private const float DAMAGE_MULTIPLIER_PER_STACK = 0.30f;

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
                    stacks = Mathf.Clamp(Mathf.RoundToInt(this.Severity), 1, MAX_STACKS);
                }
                else
                {
                    stacks = 1;
                }

                this.Severity = stacks;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_Rend.PostAdd error: {ex.Message}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                // Auto-remove after duration expires
                if (this.ageTicks >= DURATION_TICKS)
                {
                    if (pawn != null && pawn.health != null)
                    {
                        pawn.health.RemoveHediff(this);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_Rend.Tick error: {ex.Message}");
            }
        }

        /// <summary>
        /// Set the applier pawn reference.
        /// </summary>
        public void SetApplier(Pawn applier)
        {
            this.applierPawn = applier;
        }

        /// <summary>
        /// Add stacks (clamped to max 8).
        /// </summary>
        public void AddStacks(int amount)
        {
            if (amount <= 0) return;

            stacks = Mathf.Clamp(stacks + amount, 1, MAX_STACKS);
            this.Severity = stacks;

            // Reset duration
            this.ageTicks = 0;
        }

        /// <summary>
        /// Consume stacks (for No Survivors ability).
        /// </summary>
        public void ConsumeStacks(int amount)
        {
            stacks = Mathf.Max(0, stacks - amount);
            this.Severity = stacks;

            // Remove if no stacks remain
            if (stacks <= 0 && pawn != null && pawn.health != null)
            {
                pawn.health.RemoveHediff(this);
            }
        }

        /// <summary>
        /// Get the damage multiplier for this hediff.
        /// Formula: 1 + (0.30 * stacks)
        /// </summary>
        public float GetDamageMultiplier()
        {
            return 1f + (DAMAGE_MULTIPLIER_PER_STACK * stacks);
        }

        /// <summary>
        /// Save/load data.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stacks, "stacks", 0);
            Scribe_References.Look(ref applierPawn, "applierPawn");
        }

        /// <summary>
        /// Merge with existing Rend hediff.
        /// </summary>
        public override bool TryMergeWith(Hediff other)
        {
            if (other is Hediff_Rend otherRend)
            {
                // Add stacks (clamped to max)
                AddStacks(otherRend.stacks);
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

    /// <summary>
    /// HediffComp to intercept incoming damage and apply Rend multiplier.
    /// </summary>
    public class HediffComp_RendDamageMultiplier : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            // Register for damage interception
            // Note: This requires Harmony patch to Pawn.PreApplyDamage
            // See HarmonyPatches_Hestia.cs for implementation
        }
    }

    /// <summary>
    /// CompProperties for Rend damage multiplier.
    /// </summary>
    public class HediffCompProperties_RendDamageMultiplier : HediffCompProperties
    {
        public HediffCompProperties_RendDamageMultiplier()
        {
            compClass = typeof(HediffComp_RendDamageMultiplier);
        }
    }
}
