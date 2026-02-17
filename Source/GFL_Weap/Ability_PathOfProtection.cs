using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Path of Protection ability - AoE Hydro explosion with Taryz summon and ally buffs
    /// </summary>
    public class Verb_PathOfProtection : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                if (!currentTarget.HasThing || !(currentTarget.Thing is Pawn targetPawn) || CasterPawn == null || CasterPawn.Map == null)
                {
                    return false;
                }

                // Only target enemies
                if (!targetPawn.HostileTo(CasterPawn))
                {
                    Messages.Message("Must target an enemy.", MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                Map map = CasterPawn.Map;
                IntVec3 targetPos = targetPawn.Position;

                // Get weapon damage
                float weaponDamage = 20f; // Default
                Thing weapon = CasterPawn.equipment?.Primary;
                if (weapon != null && weapon.def.Verbs != null && weapon.def.Verbs.Count > 0)
                {
                    var verb = weapon.def.Verbs[0];
                    if (verb.defaultProjectile?.projectile != null)
                    {
                        weaponDamage = verb.defaultProjectile.projectile.GetDamageAmount(weapon);
                    }
                }

                float aoeDamage = weaponDamage * 0.8f; // 80% weapon damage

                // Hydro blue explosion visual
                CreateHydroExplosionEffects(targetPos, map, 3f);

                // Get all pawns within 3-tile radius
                List<Thing> affectedThings = GenRadial.RadialDistinctThingsAround(targetPos, map, 3f, true).ToList();
                List<Pawn> hitEnemies = new List<Pawn>();

                foreach (Thing thing in affectedThings)
                {
                    if (thing is Pawn pawn && !pawn.Dead && pawn != CasterPawn)
                    {
                        // Only damage enemies
                        if (!pawn.HostileTo(CasterPawn))
                        {
                            continue;
                        }

                        hitEnemies.Add(pawn);

                        // Apply Hydro damage
                        DamageDef hydroDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_HydroDamage") ?? DamageDefOf.Burn;
                        DamageInfo dinfo = new DamageInfo(hydroDmg, Mathf.RoundToInt(aoeDamage), 0f, -1f, CasterPawn);
                        
                        // Store visual references before damage
                        Vector3 pawnDrawPos = pawn.DrawPos;
                        Map pawnMap = pawn.Map;
                        
                        pawn.TakeDamage(dinfo);

                        // Only apply effects if pawn is still alive
                        if (!pawn.Dead && pawn.health != null)
                        {
                            // Apply Damp
                            ApplyDamp(pawn);

                            // Apply False Intelligence
                            HediffDef falseIntelDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_FalseIntelligence");
                            if (falseIntelDef != null)
                            {
                                Hediff falseIntel = HediffMaker.MakeHediff(falseIntelDef, pawn);
                                pawn.health.AddHediff(falseIntel);
                            }
                        }

                        // Visual feedback (use stored references)
                        if (pawnMap != null)
                        {
                            MoteMaker.ThrowText(pawnDrawPos, pawnMap, 
                                $"-{Mathf.RoundToInt(aoeDamage)}", 
                                new Color(0.5f, 0.8f, 1f), 1.5f);
                        }
                    }
                }

                // Summon Taryz to track the primary target
                SummonTaryz(targetPawn, CasterPawn, weaponDamage);

                // Grant ally buffs
                GrantAllyBuffs(CasterPawn, weaponDamage);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_PathOfProtection.TryCastShot: {ex}");
            }

            return false;
        }

        private void ApplyDamp(Pawn target)
        {
            try
            {
                if (target == null || target.Dead || target.health == null)
                {
                    return;
                }

                HediffDef dampDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Damp");
                if (dampDef == null) return;

                Hediff_Damp existing = target.health.hediffSet.GetFirstHediffOfDef(dampDef) as Hediff_Damp;
                
                if (existing != null)
                {
                    // Add stack (max 2)
                    existing.Severity = Mathf.Min(existing.Severity + 1f, 2f);
                }
                else
                {
                    // Create new with 1 stack
                    Hediff_Damp newDamp = HediffMaker.MakeHediff(dampDef, target) as Hediff_Damp;
                    if (newDamp != null)
                    {
                        newDamp.Severity = 1f;
                        target.health.AddHediff(newDamp);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error applying Damp: {ex}");
            }
        }

        private void SummonTaryz(Pawn target, Pawn caster, float attackPower)
        {
            try
            {
                HediffDef taryzDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_TaryzTracker");
                if (taryzDef == null) return;

                // Remove any existing Taryz tracker from other targets
                List<Pawn> allPawns = caster.Map.mapPawns.AllPawnsSpawned.ToList();
                foreach (Pawn pawn in allPawns)
                {
                    Hediff existing = pawn.health?.hediffSet?.GetFirstHediffOfDef(taryzDef);
                    if (existing != null)
                    {
                        pawn.health.RemoveHediff(existing);
                    }
                }

                // Apply Taryz tracker to new target
                Hediff_TaryzTracker taryz = HediffMaker.MakeHediff(taryzDef, target) as Hediff_TaryzTracker;
                if (taryz != null)
                {
                    taryz.caster = caster;
                    taryz.attackPower = attackPower;
                    target.health.AddHediff(taryz);

                    // Visual feedback
                    if (target.Map != null && target.Spawned)
                    {
                        FleckMaker.ThrowLightningGlow(target.DrawPos, target.Map, 1.5f);
                        MoteMaker.ThrowText(target.DrawPos, target.Map, "Taryz Summoned!", new Color(0.5f, 0.8f, 1f), 2.5f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error summoning Taryz: {ex}");
            }
        }

        private void GrantAllyBuffs(Pawn caster, float attackPower)
        {
            try
            {
                if (caster.Map == null) return;

                // Get all ally pawns on the current map (FULL MAP range)
                List<Pawn> allies = caster.Map.mapPawns.AllPawnsSpawned
                    .Where(p => p != null && !p.Dead && p.Faction == caster.Faction)
                    .ToList();

                foreach (Pawn ally in allies)
                {
                    // Grant Deep-Rooted Bonds
                    HediffDef bondsDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_DeepRootedBonds");
                    if (bondsDef != null)
                    {
                        Hediff_DeepRootedBonds bonds = HediffMaker.MakeHediff(bondsDef, ally) as Hediff_DeepRootedBonds;
                        if (bonds != null)
                        {
                            bonds.caster = caster;
                            bonds.healAmount = attackPower * 1.0f;
                            ally.health.AddHediff(bonds);
                        }
                    }

                    // Grant Overflowing Care
                    HediffDef careDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_OverflowingCare");
                    if (careDef != null)
                    {
                        Hediff_OverflowingCare care = HediffMaker.MakeHediff(careDef, ally) as Hediff_OverflowingCare;
                        if (care != null)
                        {
                            care.caster = caster;
                            care.healPerAction = attackPower * 0.05f;
                            ally.health.AddHediff(care);
                        }
                    }

                    // Heal ally
                    float healAmount = attackPower * 0.5f;
                    if (healAmount > 0)
                    {
                        var injury = ally.health.hediffSet.hediffs
                            .OfType<Hediff_Injury>()
                            .Where(h => h.CanHealNaturally())
                            .FirstOrDefault();

                        if (injury != null)
                        {
                            injury.Heal(healAmount);
                        }
                    }

                    // Cleanse 1 random negative hediff
                    CleanseSingleNegativeHediff(ally);

                    // Visual feedback
                    if (ally.Map != null && ally.Spawned)
                    {
                        FleckMaker.ThrowDustPuffThick(ally.DrawPos, ally.Map, 1f, new Color(0.4f, 1f, 0.8f));
                        MoteMaker.ThrowText(ally.DrawPos, ally.Map, "Protected!", new Color(0.4f, 1f, 0.8f), 2f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error granting ally buffs: {ex}");
            }
        }

        private void CleanseSingleNegativeHediff(Pawn pawn)
        {
            try
            {
                // Get all bad hediffs that can be removed
                List<Hediff> negativeHediffs = pawn.health.hediffSet.hediffs
                    .Where(h => h.def.isBad && h.def.everCurableByItem)
                    .ToList();

                if (negativeHediffs.Count > 0)
                {
                    Hediff toRemove = negativeHediffs.RandomElement();
                    pawn.health.RemoveHediff(toRemove);

                    if (pawn.Map != null && pawn.Spawned)
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Cleansed!", new Color(1f, 1f, 0.5f), 1.5f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error cleansing hediff: {ex}");
            }
        }

        private void CreateHydroExplosionEffects(IntVec3 position, Map map, float radius)
        {
            try
            {
                if (map == null) return;

                Vector3 center = position.ToVector3Shifted();

                // Hydro blue explosion flash
                FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, radius * 2.5f);

                // Blue dust puffs
                for (int i = 0; i < 6; i++)
                {
                    Vector3 offset = Rand.InsideUnitCircleVec3 * radius * 0.5f;
                    FleckMaker.ThrowDustPuffThick(center + offset, map, radius * 0.9f, new Color(0.5f, 0.8f, 1f));
                }

                // Blue smoke
                FleckMaker.ThrowSmoke(center, map, radius);

                // Blue glow
                for (int i = 0; i < 4; i++)
                {
                    FleckMaker.ThrowLightningGlow(center, map, radius * 0.9f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in CreateHydroExplosionEffects: {ex}");
            }
        }
    }
}
