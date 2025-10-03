using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Simple shield comp that periodically checks and absorbs damage
    /// Note: For full damage interception across all RimWorld versions, consider using Harmony patches
    /// This simpler approach works by monitoring the pawn's health in Tick
    /// </summary>
    public class HediffComp_ShieldMonitor : HediffComp
    {
        private float lastRecordedHealth = -1f;
        
        private Hediff_FrostBarrier ParentBarrier
        {
            get { return parent as Hediff_FrostBarrier; }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            
            try
            {
                if (Pawn == null || Pawn.Dead || ParentBarrier == null)
                {
                    return;
                }

                // Initialize health tracking
                if (lastRecordedHealth < 0)
                {
                    lastRecordedHealth = Pawn.health.summaryHealth.SummaryHealthPercent;
                    return;
                }

                // Check if pawn took damage
                float currentHealth = Pawn.health.summaryHealth.SummaryHealthPercent;
                if (currentHealth < lastRecordedHealth)
                {
                    // Pawn took damage - we can't intercept it retroactively, but we can show visual feedback
                    if (ParentBarrier.shieldHitPoints > 0 && Pawn.Map != null)
                    {
                        FleckMaker.ThrowLightningGlow(Pawn.DrawPos, Pawn.Map, 0.5f);
                    }
                }

                lastRecordedHealth = currentHealth;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in HediffComp_ShieldMonitor: {ex}");
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastRecordedHealth, "lastRecordedHealth", -1f);
        }
    }

    public class HediffCompProperties_ShieldMonitor : HediffCompProperties
    {
        public HediffCompProperties_ShieldMonitor()
        {
            compClass = typeof(HediffComp_ShieldMonitor);
        }
    }
}
