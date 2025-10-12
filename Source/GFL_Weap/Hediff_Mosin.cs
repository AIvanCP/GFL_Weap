using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Conductivity Hediff - Makes pawn vulnerable to electrical damage
    /// When taking damage, small chance to trigger paralysis
    /// </summary>
    public class Hediff_Conductivity : HediffWithComps
    {
        private const float paralysisChanceOnDamage = 0.15f; // 15% chance per damage event
        
        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"{(ticksLeft / 60f):F1}s";
                }
                catch
                {
                    return "";
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                // Visual effect when applied
                if (pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.8f);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Conductive!", new Color(0.4f, 0.8f, 1f), 1.8f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Conductivity.PostAdd: {ex}");
            }
        }

        /// <summary>
        /// Called after pawn takes damage while having this hediff
        /// Small chance to trigger paralysis
        /// </summary>
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            
            try
            {
                // Skip if pawn is dead or no damage was dealt
                if (pawn == null || pawn.Dead || totalDamageDealt <= 0)
                {
                    return;
                }

                // Small chance to trigger paralysis when taking damage
                if (Rand.Chance(paralysisChanceOnDamage))
                {
                    HediffDef paralysisDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Paralysis");
                    if (paralysisDef != null && pawn.health != null)
                    {
                        // Check if already paralyzed
                        if (!pawn.health.hediffSet.HasHediff(paralysisDef))
                        {
                            pawn.health.AddHediff(paralysisDef);
                            
                            // Visual feedback
                            if (pawn.Map != null && pawn.Spawned)
                            {
                                FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.2f);
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Paralyzed!", new Color(1f, 0.9f, 0.5f), 2f);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Conductivity.Notify_PawnPostApplyDamage: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }

    /// <summary>
    /// Paralysis Hediff - Prevents movement and manipulation for short duration
    /// </summary>
    public class Hediff_Paralysis : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"{(ticksLeft / 60f):F1}s";
                }
                catch
                {
                    return "";
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                // Visual effect when applied
                if (pawn?.Map != null && pawn.Spawned)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.6f);
                    }
                    FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.ExplosionFlash, 0.8f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Paralysis.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            try
            {
                // Occasional visual spark while paralyzed
                if (Find.TickManager.TicksGame % 30 == 0 && pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.4f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Paralysis.Tick: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
