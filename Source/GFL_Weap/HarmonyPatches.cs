using System;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Harmony patches for GFL Weapons mod - applies on game startup
    /// </summary>
    [StaticConstructorOnStartup]
    public static class GFL_HarmonyLoader
    {
        static GFL_HarmonyLoader()
        {
            try
            {
                var harmony = new Harmony("gfl.weap.patches");
                harmony.PatchAll();
                Log.Message("[GFL Weapons] Harmony patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Failed to apply Harmony patches: {ex}");
            }
        }
    }

    /// <summary>
    /// Intercept damage on pawns to allow Frost Barrier shield absorption
    /// Uses PreApplyDamage which is called before damage is applied
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class Thing_TakeDamage_Patch
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            try
            {
                // Only process pawns
                if (!(__instance is Pawn pawn))
                {
                    return;
                }

                // Skip if no damage or pawn is dead
                if (pawn.Dead || dinfo.Amount <= 0)
                {
                    return;
                }

                // Check for Frost Barrier shield
                var frostBarrierDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_FrostBarrier");
                if (frostBarrierDef == null)
                {
                    return; // No def found, continue normal damage
                }

                var frostBarrier = pawn.health?.hediffSet?.GetFirstHediffOfDef(frostBarrierDef) as Hediff_FrostBarrier;
                if (frostBarrier == null || frostBarrier.shieldHitPoints <= 0)
                {
                    return; // No shield or shield depleted, continue normal damage
                }

                // Shield is active - absorb damage
                float damageAmount = dinfo.Amount;

                if (damageAmount <= frostBarrier.shieldHitPoints)
                {
                    // Shield absorbs all damage
                    frostBarrier.shieldHitPoints -= damageAmount;
                    dinfo.SetAmount(0);

                    // Visual feedback
                    if (pawn.Map != null && pawn.Spawned)
                    {
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.6f);
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, 
                            $"-{Mathf.RoundToInt(damageAmount)}", 
                            new Color(0.5f, 0.8f, 1f), 1.5f);
                    }
                }
                else
                {
                    // Shield absorbs partial damage, then breaks
                    float remainingDamage = damageAmount - frostBarrier.shieldHitPoints;
                    frostBarrier.shieldHitPoints = 0;
                    dinfo.SetAmount(remainingDamage);

                    // Shield broken visual
                    if (pawn.Map != null && pawn.Spawned)
                    {
                        FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.ExplosionFlash, 1f);
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, 
                            "Barrier Broken!", 
                            Color.red, 2f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Thing_TakeDamage_Patch: {ex}");
            }
        }
    }
}
