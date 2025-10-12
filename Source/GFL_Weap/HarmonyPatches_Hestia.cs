using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GFL_Weap
{
    /// <summary>
    /// Harmony patches for Hestia weapon system.
    /// Implements Rend damage multiplier interception.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HarmonyPatches_Hestia
    {
        static HarmonyPatches_Hestia()
        {
            try
            {
                Harmony harmony = new Harmony("com.gflweap.hestia");
                
                // Patch PreApplyDamage to apply Rend multiplier
                harmony.Patch(
                    AccessTools.Method(typeof(Pawn), nameof(Pawn.PreApplyDamage)),
                    prefix: new HarmonyMethod(typeof(HarmonyPatches_Hestia), nameof(PreApplyDamage_Prefix))
                );

                Log.Message("[GFL Weapons] Hestia Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Failed to apply Hestia Harmony patches: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Prefix patch for Pawn.PreApplyDamage to apply Rend damage multiplier.
        /// </summary>
        public static void PreApplyDamage_Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            try
            {
                if (__instance == null || __instance.Dead || __instance.health == null)
                {
                    return;
                }

                // Check for Rend hediff
                Hediff_Rend rend = __instance.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("GFL_Hediff_Rend")) as Hediff_Rend;
                if (rend == null)
                {
                    return;
                }

                // Only apply multiplier to physical damage types
                if (dinfo.Def == DamageDefOf.Bullet || 
                    dinfo.Def == DamageDefOf.Cut || 
                    dinfo.Def == DamageDefOf.Stab || 
                    dinfo.Def == DamageDefOf.Blunt ||
                    dinfo.Def == DamageDefOf.Scratch ||
                    dinfo.Def == DamageDefOf.Bite ||
                    dinfo.Def == DamageDefOf.Crush)
                {
                    // Apply Rend multiplier
                    float multiplier = rend.GetDamageMultiplier();
                    float originalAmount = dinfo.Amount;
                    dinfo.SetAmount(originalAmount * multiplier);

                    // Log for debugging (can be removed in production)
                    // Log.Message($"[GFL Weapons] Rend multiplier applied: {originalAmount} * {multiplier} = {dinfo.Amount}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] PreApplyDamage_Prefix exception: {ex.Message}");
            }
        }
    }
}
