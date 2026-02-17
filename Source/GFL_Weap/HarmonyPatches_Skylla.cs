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
    /// Harmony patch to handle Toxic Infiltration death explosions
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_ToxicInfiltration_Patch
    {
        private static HashSet<Pawn> currentlyExplodingPawns = new HashSet<Pawn>();
        private static int chainCount = 0;
        private static int lastChainTick = 0;

        public static void Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            try
            {
                // Skip if already exploding (prevent infinite loop)
                if (currentlyExplodingPawns.Contains(__instance))
                {
                    return;
                }

                // Check for Toxic Infiltration
                var toxicInfiltrationDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_ToxicInfiltration");
                if (toxicInfiltrationDef == null) return;

                var toxicInfiltration = __instance.health?.hediffSet?.GetFirstHediffOfDef(toxicInfiltrationDef) as Hediff_ToxicInfiltration;
                if (toxicInfiltration == null) return;

                // Pawn has Toxic Infiltration and is dying - trigger explosion
                Map map = __instance.Map;
                IntVec3 position = __instance.Position;
                Pawn attacker = toxicInfiltration.originalAttacker;

                if (map == null) return;

                // Reset chain counter if enough time has passed (30 ticks = 0.5 seconds)
                int currentTick = Find.TickManager.TicksGame;
                if (currentTick - lastChainTick > 30)
                {
                    chainCount = 0;
                }
                lastChainTick = currentTick;

                // Limit chain to max 10 explosions per 0.5 second period
                if (chainCount >= 10)
                {
                    return;
                }

                chainCount++;
                currentlyExplodingPawns.Add(__instance);

                // Trigger purple explosion IMMEDIATELY (not delayed)
                Map mapRef = map;
                IntVec3 posRef = position;
                Pawn attackerRef = attacker;
                Pawn dyingPawn = __instance;

                // Execute explosion immediately on next tick to ensure visibility
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; // Ensure not paused
                
                // Queue explosion for next tick
                MapComponent_SkyllaExplosions explosionComp = map.GetComponent<MapComponent_SkyllaExplosions>();
                if (explosionComp == null)
                {
                    explosionComp = new MapComponent_SkyllaExplosions(map);
                    map.components.Add(explosionComp);
                }
                explosionComp.QueueExplosion(posRef, attackerRef, dyingPawn);
                
                // Remove from exploding set after short delay
                currentlyExplodingPawns.Remove(dyingPawn);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Pawn_Kill_ToxicInfiltration_Patch: {ex}");
            }
        }
    }

    /// <summary>
    /// Map component to handle delayed toxic explosions on next tick
    /// </summary>
    public class MapComponent_SkyllaExplosions : MapComponent
    {
        private struct QueuedExplosion
        {
            public IntVec3 position;
            public Pawn attacker;
            public Pawn dyingPawn;
        }

        private List<QueuedExplosion> queuedExplosions = new List<QueuedExplosion>();

        public MapComponent_SkyllaExplosions(Map map) : base(map)
        {
        }

        public void QueueExplosion(IntVec3 position, Pawn attacker, Pawn dyingPawn)
        {
            queuedExplosions.Add(new QueuedExplosion
            {
                position = position,
                attacker = attacker,
                dyingPawn = dyingPawn
            });
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (queuedExplosions.Count > 0)
            {
                foreach (var explosion in queuedExplosions)
                {
                    TriggerExplosion(explosion.position, explosion.attacker, explosion.dyingPawn);
                }
                queuedExplosions.Clear();
            }
        }

        private void TriggerExplosion(IntVec3 position, Pawn attacker, Pawn dyingPawn)
        {
            try
            {
                // VIBRANT PURPLE explosion visual effects (no sparks - chain explosion)
                SkyllaExplosionContext.IsProjectileTrigger = false;
                SkyllaUtility.DoCorrosionExplosion(position, map, 6f, attacker);
                
                // Camera shake for dramatic effect
                if (map == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(0.8f);
                }

                // Apply Toxic Infiltration to all enemies within 6 tiles
                List<Thing> affectedThings = GenRadial.RadialDistinctThingsAround(position, map, 6f, true).ToList();

                foreach (Thing thing in affectedThings)
                {
                    if (thing is Pawn targetPawn && !targetPawn.Dead && targetPawn != dyingPawn)
                    {
                        // CRITICAL: Only affect enemies, never player faction
                        if (attacker != null)
                        {
                            if (targetPawn.Faction?.IsPlayer == true || !targetPawn.HostileTo(attacker))
                            {
                                continue;
                            }
                        }
                        else if (dyingPawn?.Faction != null)
                        {
                            if (targetPawn.Faction?.IsPlayer == true || !targetPawn.HostileTo(dyingPawn.Faction))
                            {
                                continue;
                            }
                        }

                        // Null safety check
                        if (targetPawn.health == null) continue;

                        // Apply Toxic Infiltration hediff
                        HediffDef toxicInfiltrationDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_ToxicInfiltration");
                        if (toxicInfiltrationDef != null)
                        {
                            Hediff_ToxicInfiltration existing = targetPawn.health.hediffSet.GetFirstHediffOfDef(toxicInfiltrationDef) as Hediff_ToxicInfiltration;
                            
                            if (existing != null)
                            {
                                existing.ageTicks = 0;
                            }
                            else
                            {
                                Hediff_ToxicInfiltration newHediff = HediffMaker.MakeHediff(toxicInfiltrationDef, targetPawn) as Hediff_ToxicInfiltration;
                                if (newHediff != null)
                                {
                                    newHediff.originalAttacker = attacker;
                                    targetPawn.health.AddHediff(newHediff);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in MapComponent explosion trigger: {ex}");
            }
        }
    }
}
