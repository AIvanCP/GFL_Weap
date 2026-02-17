using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Harmony patch to handle Taryz Tracker counterattack on attack attempt (hit or miss)
    /// Hooks into job start to catch all attack attempts
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "TryStartAttack")]
    public static class Pawn_TryStartAttack_TaryzTracker_Patch
    {
        public static void Postfix(Pawn __instance, LocalTargetInfo targ, bool __result)
        {
            try
            {
                // Only process if attack was started
                if (!__result) return;

                // Get the attacker pawn
                Pawn attacker = __instance;
                if (attacker == null || attacker.Dead) return;

                // Check if attacker has Taryz Tracker hediff
                HediffDef taryzDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_TaryzTracker");
                if (taryzDef == null) return;

                Hediff_TaryzTracker taryzHediff = attacker.health?.hediffSet?.GetFirstHediffOfDef(taryzDef) as Hediff_TaryzTracker;
                if (taryzHediff == null) return;

                // Check if target is an ally of the caster
                if (!(targ.Thing is Pawn targetPawn) || targetPawn.Dead) return;

                Pawn caster = taryzHediff.caster;
                if (caster == null || caster.Dead) return;

                // If tracked enemy attacked an ally (hit OR miss), trigger counterattack
                if (targetPawn.Faction == caster.Faction)
                {
                    TriggerTaryzCounterattack(attacker, targetPawn, taryzHediff.attackPower, caster);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Pawn_TryStartAttack_TaryzTracker_Patch: {ex}");
            }
        }

        private static void TriggerTaryzCounterattack(Pawn trackedEnemy, Pawn allyTarget, float attackPower, Pawn caster)
        {
            try
            {
                if (trackedEnemy == null || trackedEnemy.Dead) return;

                // Deal Hydro damage to tracked enemy (10-20 base)
                int counterDamage = Rand.Range(10, 20);
                float scaledDamage = counterDamage * (attackPower / 20f);

                DamageDef hydroDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_HydroDamage") ?? DamageDefOf.Burn;
                DamageInfo dinfo = new DamageInfo(hydroDmg, Mathf.RoundToInt(scaledDamage), 0f, -1f, caster);
                trackedEnemy.TakeDamage(dinfo);

                // Heal the ally who was attacked
                if (allyTarget != null && !allyTarget.Dead && allyTarget.health != null)
                {
                    var injury = allyTarget.health.hediffSet.hediffs
                        .OfType<Hediff_Injury>()
                        .Where(h => h.CanHealNaturally())
                        .FirstOrDefault();

                    if (injury != null)
                    {
                        float healAmount = attackPower * 0.3f;
                        injury.Heal(healAmount);

                        if (allyTarget.Map != null && allyTarget.Spawned)
                        {
                            MoteMaker.ThrowText(allyTarget.DrawPos, allyTarget.Map, 
                                $"+{Mathf.RoundToInt(healAmount)}", 
                                new Color(0.4f, 1f, 0.8f), 1.5f);
                        }
                    }
                }

                // Visual feedback
                if (trackedEnemy.Map != null && trackedEnemy.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(trackedEnemy.DrawPos, trackedEnemy.Map, 1.2f);
                    MoteMaker.ThrowText(trackedEnemy.DrawPos, trackedEnemy.Map, 
                        "Taryz Strike!", 
                        new Color(0.5f, 0.8f, 1f), 2f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in TriggerTaryzCounterattack: {ex}");
            }
        }
    }
}
