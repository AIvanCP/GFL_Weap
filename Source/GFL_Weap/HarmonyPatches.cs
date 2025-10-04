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
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "TakeDamage")]
    public static class Pawn_TakeDamage_Patch
    {
        public static bool Prefix(Pawn __instance, ref DamageInfo dinfo)
        {
            try
            {
                // Skip if no damage or pawn is dead
                if (__instance == null || __instance.Dead || dinfo.Amount <= 0)
                {
                    return true;
                }

                // Check for Frost Barrier shield
                var frostBarrierDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_FrostBarrier");
                if (frostBarrierDef == null)
                {
                    return true; // No def found, continue normal damage
                }

                var frostBarrier = __instance.health?.hediffSet?.GetFirstHediffOfDef(frostBarrierDef) as Hediff_FrostBarrier;
                if (frostBarrier == null || frostBarrier.shieldHitPoints <= 0)
                {
                    return true; // No shield or shield depleted, continue normal damage
                }

                // Shield is active - absorb damage
                float damageAmount = dinfo.Amount;

                if (damageAmount <= frostBarrier.shieldHitPoints)
                {
                    // Shield absorbs all damage
                    frostBarrier.shieldHitPoints -= damageAmount;
                    dinfo.SetAmount(0);

                    // Visual feedback
                    if (__instance.Map != null && __instance.Spawned)
                    {
                        FleckMaker.ThrowLightningGlow(__instance.DrawPos, __instance.Map, 0.6f);
                        MoteMaker.ThrowText(__instance.DrawPos, __instance.Map, 
                            $"-{Mathf.RoundToInt(damageAmount)}", 
                            new Color(0.5f, 0.8f, 1f), 1.5f);
                    }

                    return false; // Block original damage application
                }
                else
                {
                    // Shield absorbs partial damage, then breaks
                    float remainingDamage = damageAmount - frostBarrier.shieldHitPoints;
                    frostBarrier.shieldHitPoints = 0;
                    dinfo.SetAmount(remainingDamage);

                    // Shield broken visual
                    if (__instance.Map != null && __instance.Spawned)
                    {
                        FleckMaker.Static(__instance.DrawPos, __instance.Map, FleckDefOf.ExplosionFlash, 1f);
                        MoteMaker.ThrowText(__instance.DrawPos, __instance.Map, 
                            "Barrier Broken!", 
                            Color.red, 2f);
                    }

                    return true; // Apply remaining damage
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Pawn_TakeDamage_Patch: {ex}");
                return true; // Continue with original damage on error
            }
        }
    }
}
