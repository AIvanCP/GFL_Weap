using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Confectance Index hediff - Simple stack storage for combat momentum.
    /// Used by Hestia weapon abilities to track accumulated advantage.
    /// </summary>
    public class Hediff_ConfectanceIndex : HediffWithComps
    {
        // Stack count storage
        private int stacks = 0;

        // Duration in ticks (60 seconds = 3600 ticks)
        private const int DURATION_TICKS = 3600;

        public int Stacks => stacks;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            try
            {
                // Initialize from severity if needed
                if (this.Severity > 0)
                {
                    stacks = Mathf.Max(0, Mathf.RoundToInt(this.Severity));
                }

                if (stacks < 1)
                {
                    stacks = 1;
                }

                this.Severity = stacks;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Hediff_ConfectanceIndex.PostAdd error: {ex.Message}");
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
                Log.Error($"[GFL Weapons] Hediff_ConfectanceIndex.Tick error: {ex.Message}");
            }
        }

        /// <summary>
        /// Add stacks to the Confectance Index.
        /// </summary>
        public void AddStacks(int amount)
        {
            if (amount <= 0) return;

            stacks += amount;
            this.Severity = stacks;

            // Reset duration
            this.ageTicks = 0;
        }

        /// <summary>
        /// Get current stack count.
        /// </summary>
        public int GetStacks()
        {
            return stacks;
        }

        /// <summary>
        /// Consume all stacks and return the amount consumed.
        /// </summary>
        public int ConsumeAll()
        {
            int consumed = stacks;
            stacks = 0;
            this.Severity = 0;

            // Remove hediff if no stacks remain
            if (pawn != null && pawn.health != null)
            {
                pawn.health.RemoveHediff(this);
            }

            return consumed;
        }

        /// <summary>
        /// Save/load stack data.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stacks, "stacks", 0);
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
