using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Devastating Drift ability - dash with explosion trail
    /// </summary>
    public class Verb_DevastatingDrift : Verb_CastAbility
    {
        private static int extraCastGranted = 0;
        private static int lastCastTick = 0;

        protected override bool TryCastShot()
        {
            try
            {
                if (!currentTarget.IsValid || CasterPawn == null || CasterPawn.Map == null)
                {
                    return false;
                }

                IntVec3 targetCell = currentTarget.Cell;
                Map map = CasterPawn.Map;

                // Validate target is walkable and within range
                if (!targetCell.Walkable(map) || !targetCell.InBounds(map))
                {
                    Messages.Message("Target location is not walkable.", MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                float distance = CasterPawn.Position.DistanceTo(targetCell);
                if (distance < 4f || distance > 28f)
                {
                    Messages.Message("Target must be 4-28 tiles away.", MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // Create straight line path from caster to target
                List<IntVec3> pathCells = new List<IntVec3>();
                
                // Get cells along line
                foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(CasterPawn.Position, targetCell))
                {
                    if (cell.InBounds(map) && cell.Walkable(map))
                    {
                        pathCells.Add(cell);
                    }
                    else if (!cell.Walkable(map))
                    {
                        // Stop at walls
                        break;
                    }
                }

                if (pathCells.Count < 2)
                {
                    Messages.Message("Cannot dash to target (blocked).", MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // Execute dash
                ExecuteDash(pathCells, map, CasterPawn);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_DevastatingDrift.TryCastShot: {ex}");
            }

            return false;
        }

        private void ExecuteDash(List<IntVec3> pathCells, Map map, Pawn caster)
        {
            try
            {
                // Get weapon damage
                float weaponDamage = 15f; // Default
                Thing weapon = caster.equipment?.Primary;
                if (weapon != null && weapon.def.Verbs != null && weapon.def.Verbs.Count > 0)
                {
                    var verb = weapon.def.Verbs[0];
                    if (verb.defaultProjectile?.projectile != null)
                    {
                        weaponDamage = verb.defaultProjectile.projectile.GetDamageAmount(weapon);
                    }
                }

                int killCount = 0;
                IntVec3 startPos = caster.Position;

                // Process each cell in the dash path
                foreach (IntVec3 cell in pathCells)
                {
                    if (cell == startPos) continue; // Skip starting position

                    // Move pawn to this cell instantly
                    caster.Position = cell;
                    caster.Notify_Teleported(true, true);
                    
                    // Create 5-tile-wide explosion at this position
                    List<IntVec3> explosionCells = GenRadial.RadialCellsAround(cell, 2.5f, true).ToList(); // 2.5 radius = 5 tile width

                    // Show purple explosion on EVERY tile in blast radius (once per tile, no overlap)
                    SkyllaUtility.DoCorrosionExplosion_Ultimate(cell, map, 2.5f);

                    foreach (IntVec3 explosionCell in explosionCells)
                    {
                        if (!explosionCell.InBounds(map)) continue;

                        // Damage all enemies in this cell
                        List<Thing> things = explosionCell.GetThingList(map).ToList();
                        foreach (Thing thing in things)
                        {
                            if (thing is Pawn targetPawn && !targetPawn.Dead && targetPawn != caster)
                            {
                                // CRITICAL: Safe for allies and player faction - only damage enemies
                                if (targetPawn.Faction?.IsPlayer == true || !targetPawn.HostileTo(caster))
                                {
                                    continue;
                                }

                                bool wasAlive = !targetPawn.Dead;
                                
                                // Apply 100% weapon damage
                                DamageDef corrosionDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_CorrosionDamage") ?? DamageDefOf.Burn;
                                DamageInfo dinfo = new DamageInfo(corrosionDmg, Mathf.RoundToInt(weaponDamage), 0f, -1f, caster);
                                targetPawn.TakeDamage(dinfo);

                                // Apply Toxic Infiltration
                                ApplyToxicInfiltration(targetPawn, caster);

                                // Count kills
                                if (wasAlive && targetPawn.Dead)
                                {
                                    killCount++;
                                }

                                // Visual feedback
                                if (targetPawn.Map != null && targetPawn.Spawned)
                                {
                                    MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, 
                                        $"-{Mathf.RoundToInt(weaponDamage)}", 
                                        new Color(1f, 0.3f, 1f), 1.5f);
                                }
                            }
                        }
                    }

                    // Minimal visual for dash trail - no extra dust to avoid rotation issues
                }

                // Grant extra cast if 2+ enemies killed (only once per cast)
                int currentTick = Find.TickManager.TicksGame;
                if (killCount >= 2 && currentTick - lastCastTick > 60 && extraCastGranted == 0)
                {
                    extraCastGranted = 1;
                    lastCastTick = currentTick;
                    
                    // Reset cooldown for one extra cast
                    if (caster.abilities != null)
                    {
                        var ability = caster.abilities.GetAbility(DefDatabase<AbilityDef>.GetNamedSilentFail("GFL_Ability_DevastatingDrift"));
                        if (ability != null)
                        {
                            ability.StartCooldown(1); // Set cooldown to 1 tick (instant recast)
                            Messages.Message($"{caster.LabelShort}: Devastating Drift recharged! ({killCount} kills)", MessageTypeDefOf.PositiveEvent, false);
                        }
                    }
                }
                else if (currentTick - lastCastTick > 60)
                {
                    // Reset extra cast counter for new activation
                    extraCastGranted = 0;
                    lastCastTick = currentTick;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in ExecuteDash: {ex}");
            }
        }

        private void ApplyToxicInfiltration(Pawn target, Pawn attacker)
        {
            try
            {
                // Null safety checks
                if (target == null || target.Dead || target.health == null)
                    return;

                HediffDef toxicInfiltrationDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_ToxicInfiltration");
                if (toxicInfiltrationDef == null) return;

                Hediff_ToxicInfiltration existing = target.health.hediffSet.GetFirstHediffOfDef(toxicInfiltrationDef) as Hediff_ToxicInfiltration;
                
                if (existing != null)
                {
                    // Refresh duration
                    existing.ageTicks = 0;
                }
                else
                {
                    // Apply new hediff
                    Hediff_ToxicInfiltration newHediff = HediffMaker.MakeHediff(toxicInfiltrationDef, target) as Hediff_ToxicInfiltration;
                    if (newHediff != null)
                    {
                        newHediff.originalAttacker = attacker;
                        target.health.AddHediff(newHediff);
                    }
                }

                // Also add Corrosive Infusion stack
                HediffDef corrosiveInfusionDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_CorrosiveInfusion");
                if (corrosiveInfusionDef != null)
                {
                    Hediff_CorrosiveInfusion existingInfusion = target.health.hediffSet.GetFirstHediffOfDef(corrosiveInfusionDef) as Hediff_CorrosiveInfusion;
                    
                    if (existingInfusion != null)
                    {
                        existingInfusion.Severity = Mathf.Min(existingInfusion.Severity + 1f, 10f);
                        existingInfusion.originalAttacker = attacker;
                    }
                    else
                    {
                        Hediff_CorrosiveInfusion newInfusion = HediffMaker.MakeHediff(corrosiveInfusionDef, target) as Hediff_CorrosiveInfusion;
                        if (newInfusion != null)
                        {
                            newInfusion.Severity = 1f;
                            newInfusion.originalAttacker = attacker;
                            target.health.AddHediff(newInfusion);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error applying Toxic Infiltration: {ex}");
            }
        }
    }
}
